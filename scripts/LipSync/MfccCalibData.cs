using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class MfccCalibData : Resource
{
    public float[] mfcc = new float[12];
    public float[] mean = new float[12];
    public float[] stdDev = new float[12];
    [Export]
    public Array<MfccData> calibrationData = new Array<MfccData>();

    public void UpdateData()
    {
        for (int i = 0; i < 12; i++)
        {
            mfcc[i] = 0f;
            foreach (var data in calibrationData)
            {
                mfcc[i] += data.array[i];
            }
            mfcc[i] /= calibrationData.Count;
        }
        for (int i = 0; i < 12; i++)
        {
            mean[i] = mfcc.Average();
            stdDev[i] = Mathf.Sqrt(mfcc.Select(x => Mathf.Pow(mean[i] - x, 2f)).Average());
        }
    }
}