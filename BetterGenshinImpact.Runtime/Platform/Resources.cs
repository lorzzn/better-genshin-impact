using BetterGenshinImpact.Runtime;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;

namespace BetterGenshinImpact.Core.Recognition.ONNX
{
    public sealed class BgiOnnxFactory
    {
        public InferenceSession CreateInferenceSession(BgiOnnxModel model, bool ocr = false)
        {
            using var options = new SessionOptions();
            return new InferenceSession(model.ModalPath, options);
        }
    }
}

namespace BetterGenshinImpact.Core.Recognition.OCR
{
    public static class OcrFactory
    {
        public static IOcrService Paddle => RuntimeEnvironment.Paddle;
    }
}

namespace BetterGenshinImpact.GameTask
{
    public static class GameTaskManager
    {
        public static Mat LoadAssetImage(string task, string name, ImreadModes flags = ImreadModes.Color) => LoadAssetImage(task, name, GameSession.Current.SystemInfo, flags);
        public static Mat LoadAssetImage(string task, string name, GameSystemInfo info, ImreadModes flags = ImreadModes.Color) => LoadAssetImage(task, name, info.Width, info.Height, flags);
        public static Mat LoadAssetImage(string task, string name, int width, int height, ImreadModes flags = ImreadModes.Color)
        {
            var folder = Core.Config.Global.Absolute($"GameTask/{task}/Assets/{width}x{height}");
            if (!Directory.Exists(folder)) folder = Core.Config.Global.Absolute($"GameTask/{task}/Assets/1920x1080");
            var path = Path.Combine(folder, name);
            if (!File.Exists(path)) throw new FileNotFoundException("Recognition asset missing", path);
            var mat = Cv2.ImRead(path, flags);
            if (mat.Empty()) { mat.Dispose(); throw new InvalidDataException($"Invalid recognition asset: {task}/{name}"); }
            if (width >= 1920) return mat;
            using (mat) return Core.Recognition.OpenCv.ResizeHelper.Resize(mat, width / 1920d);
        }
    }
}
