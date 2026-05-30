using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class LipSyncProfile : Resource
{
    [Export]
    public int sampleRate = 16000;
    [Export]
    public int mfccNum = 12;
    [Export]
    public int melFilterBankChannels = 30;
    [Export]
    public bool useStandardization = false;

    [Export]
    public Array<MfccCalibData> mfccDatas = new Array<MfccCalibData>();

    public void FromJson(string json)
    {
        mfccDatas.Clear();
        var dict = JsonNode.Parse(json);
        foreach (var phoneme in dict["mfccs"].AsArray())
        {
            string name = phoneme["name"].ToString();
            var dataList = phoneme["mfccCalibrationDataList"].AsArray();
            Array<float[]> datas = new Array<float[]>();
            MfccCalibData final = new MfccCalibData()
            {
                ResourceName = name,
            };
            foreach (var data in dataList)
            {
                float[] arr = data["array"].AsArray().GetValues<float>().ToArray();
                datas.Add(arr);
                final.calibrationData.Add(new MfccData()
                {
                    array = arr,
                });
            }
            mfccDatas.Add(final);
            final.UpdateData();
        }
    }
}