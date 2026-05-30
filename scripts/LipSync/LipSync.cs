using System.Linq;
using Godot;
using Godot.Collections;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using NWaves.Filters.Base;
using NWaves.Filters.Fda;
using NWaves.Operations;
using NWaves.Signals;
using NWaves.Transforms;
using NWaves.Windows;

[GlobalClass]
[Tool]
public partial class LipSync : Node
{
    public enum ScoreCalcType
    {
        L1,
        L2,
        Cosine,
    }

    [Export]
    public bool speaking = true;

    [Export]
    public MeshInstance3D mesh;
    [Export]
    public Label3D debugLabel;
    [Export]
    public int busIndex;
    [Export]
    public int effectIndex;

    [Export]
    public float signalGain = 1f;
    [Export]
    public float rmsMultiplier = 1f;
    [Export]
    public float lerpSpeed = 1f;
    [Export]
    public ScoreCalcType scoreType = ScoreCalcType.L1;
    [Export]
    public Dictionary<string, BlendShapeData> phonemeToBlendShapes = new Dictionary<string, BlendShapeData>();
    [Export]
    public LipSyncProfile profile;

    [Export(PropertyHint.GlobalFile)]
    public string jsonPath = string.Empty;
    [ExportToolButton("Import Profile from Json Path")]
    public Callable ImportProfileButton => Callable.From(ImportProfile);

    private MfccOptions opts;
    private MfccExtractor extractor;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;
        int fftSize = 1024;
        int melCount = profile.melFilterBankChannels;
        opts = new MfccOptions()
        {
            SamplingRate = profile.sampleRate,
            FeatureCount = profile.mfccNum,
            FrameSize = fftSize,
            HopSize = 512,
            PreEmphasis = 0.97d,
            FilterBankSize = melCount,
            SpectrumType = SpectrumType.Power,
            NonLinearity = NonLinearityType.ToDecibel,
            DctType = "2N",
            FftSize = fftSize,
            Window = WindowType.Hamming,
            LogFloor = 1e-10f,
        };
        extractor = new MfccExtractor(opts);
        foreach (var data in profile.mfccDatas)
        {
            data.UpdateData();
        }
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
        }
        else if (speaking)
        {
            // AudioEffectSpectrumAnalyzerInstance analyzer = (AudioEffectSpectrumAnalyzerInstance)AudioServer.GetBusEffectInstance(busIndex, effectIndex);
            // float lastFreq = minFreq;

            AudioEffectCapture capture = AudioServer.GetBusEffect(busIndex, effectIndex) as AudioEffectCapture;
            if (!IsInstanceValid(capture))
                return;

            int fftSize = extractor.FrameSize;
            int sampleRate = profile.sampleRate;
            int bufRatio = (int)Mathf.Max(1, AudioServer.GetInputMixRate() / sampleRate);
            if (capture.CanGetBuffer(fftSize * bufRatio))
            {
                float[] buffer = capture.GetBuffer(fftSize * bufRatio).Select(x => x.Length() * signalGain).ToArray();
                var inputSignal = new DiscreteSignal((int)AudioServer.GetInputMixRate(), buffer);
                // var signal = Operation.Resample(inputSignal, sampleRate);
                // var resampler = new Resampler();
                // var signal = resampler.Resample(inputSignal, sampleRate);
                float[] outBuffer = VoiceChat.Resample(buffer, (int)AudioServer.GetInputMixRate(), sampleRate);
                var signal = new DiscreteSignal(sampleRate, outBuffer);
                // GD.Print(signal.Length);
                var mfccVector = extractor.ComputeFrom(signal.Samples);
                float rms = signal.Rms() * rmsMultiplier;
                // int k = 0;
                // GD.Print(mfccVector.Count);
                for (int k = 0; k < mfccVector.Count; k++)
                {
                    int n = 12;
                    int profileCount = profile.mfccDatas.Count;
                    float[] scores = new float[profileCount];
                    Array<float> phonemes = new Array<float>();
                    int phonemeCount = profile.mfccNum * profileCount;
                    int index = 0;
                    for (int i = 0; i < profile.mfccDatas.Count; i++)
                    {
                        scores[i] = 0f;
                        foreach (var value in profile.mfccDatas[i].mfcc)
                        {
                            if (index >= phonemeCount)
                                break;
                            phonemes.Add(value);
                        }
                    }
                    for (int p = 0; p < profileCount; p++)
                    {
                        var phonemeSlice = phonemes.GetSliceRange(p * n, p * n + n);
                        // calculate the scores from the mean
                        // l1
                        if (scoreType == ScoreCalcType.L1)
                        {
                            float distance = 0f;
                            for (int i = 0; i < n; i++)
                            {
                                float stdDev = profile.mfccDatas[p].stdDev[i];
                                if (!profile.useStandardization)
                                    stdDev = 1f;
                                float x = (mfccVector[k][i] - profile.mfccDatas[p].mean[i]) / stdDev;
                                float y = (phonemeSlice[i] - profile.mfccDatas[p].mean[i]) / stdDev;
                                distance += Mathf.Abs(x - y);
                            }
                            scores[p] = Mathf.Pow(10f, -distance / n);
                        }
                        // l2
                        else if (scoreType == ScoreCalcType.L2)
                        {
                            float distance = 0f;
                            for (int i = 0; i < n; i++)
                            {
                                float stdDev = profile.mfccDatas[p].stdDev[i];
                                if (!profile.useStandardization)
                                    stdDev = 1f;
                                float x = (mfccVector[k][i] - profile.mfccDatas[p].mean[i]) / stdDev;
                                float y = (phonemeSlice[i] - profile.mfccDatas[p].mean[i]) / stdDev;
                                distance += Mathf.Pow(x - y, 2f);
                            }
                            distance = Mathf.Sqrt(distance / n);
                            scores[p] = Mathf.Pow(10f, -distance);
                        }
                        // cosine similarity
                        else if (scoreType == ScoreCalcType.Cosine)
                        {
                            float mfccNorm = 0f;
                            float phonemeNorm = 0f;
                            float prod = 0f;
                            for (int i = 0; i < n; i++)
                            {
                                float stdDev = profile.mfccDatas[p].stdDev[i];
                                if (!profile.useStandardization)
                                    stdDev = 1f;
                                float x = (mfccVector[k][i] - profile.mfccDatas[p].mean[i]) / stdDev;
                                float y = (phonemeSlice[i] - profile.mfccDatas[p].mean[i]) / stdDev;
                                mfccNorm += x * x;
                                phonemeNorm += y * y;
                                prod += x * y;
                            }
                            mfccNorm = Mathf.Sqrt(mfccNorm);
                            phonemeNorm = Mathf.Sqrt(phonemeNorm);
                            float similarity = prod / (mfccNorm * phonemeNorm);
                            similarity = Mathf.Max(similarity, 0f);
                            scores[p] = Mathf.Pow(similarity, 100f);
                        }
                    }
                    float sum = scores.Sum();
                    for (int i = 0; i < scores.Length; i++)
                    {
                        scores[i] = sum > 0f ? scores[i] / sum : 0f;
                    }
                    int finalIndex = -1;
                    float finalValue = -1f;
                    string finalPhoneme = string.Empty;
                    for (int i = 0; i < scores.Length; i++)
                    {
                        if (scores[i] > finalValue)
                        {
                            finalIndex = i;
                            finalValue = scores[i];
                            finalPhoneme = profile.mfccDatas[i].ResourceName;
                        }
                    }
                    // GD.PrintS(finalPhoneme, finalValue, rms);
                    foreach (var kvp in phonemeToBlendShapes)
                    {
                        if (kvp.Key == finalPhoneme)
                        {
                            continue;
                        }
                        foreach (var shape in kvp.Value.blendShapeValues)
                        {
                            int idx = mesh.FindBlendShapeByName(shape.Key);
                            float val = mesh.GetBlendShapeValue(idx);
                            val = Mathf.Lerp(val, 0f, Mathf.Clamp((float)delta * lerpSpeed, 0f, 1f));
                            val = Mathf.Clamp(val, -1f, 1f);
                            mesh.SetBlendShapeValue(idx, val);
                        }
                    }
                    if (phonemeToBlendShapes.TryGetValue(finalPhoneme, out var blendShapes))
                    {
                        foreach (var shape in blendShapes.blendShapeValues)
                        {
                            int idx = mesh.FindBlendShapeByName(shape.Key);
                            float val = mesh.GetBlendShapeValue(idx);
                            val = Mathf.Lerp(val, shape.Value, Mathf.Clamp((float)delta * lerpSpeed, 0f, 1f));
                            val = Mathf.Clamp(val, -1f, 1f);
                            mesh.SetBlendShapeValue(idx, val);
                        }
                    }
                    if (IsInstanceValid(debugLabel))
                        debugLabel.Text = finalPhoneme;
                }
            }
        }
        else
        {
            foreach (var kvp in phonemeToBlendShapes)
            {
                foreach (var shape in kvp.Value.blendShapeValues)
                {
                    int idx = mesh.FindBlendShapeByName(shape.Key);
                    float val = mesh.GetBlendShapeValue(idx);
                    val = Mathf.Lerp(val, 0f, Mathf.Clamp((float)delta * lerpSpeed, 0f, 1f));
                    val = Mathf.Clamp(val, -1f, 1f);
                    mesh.SetBlendShapeValue(idx, val);
                }
            }
        }
    }

    public void ImportProfile()
    {
        profile.FromJson(FileAccess.GetFileAsString(jsonPath));
    }
}