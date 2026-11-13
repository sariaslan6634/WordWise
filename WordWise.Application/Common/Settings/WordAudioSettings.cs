namespace WordWise.Application.Common.Settings
{
    /// <summary>Telaffuz ses dosyaları (appsettings: "WordAudio").</summary>
    public class WordAudioSettings
    {
        public const string SectionName = "WordAudio";

        /// <summary>Ses dosyalarının klasörü. Boşsa proje klasöründeki Data/wiktionary-audios aranır.</summary>
        public string Folder { get; set; } = string.Empty;

        /// <summary>
        /// Kaydı hiçbir yerde bulunamayan kelimeleri üretmek için yerel metin-okuma programı (ör. Piper'ın "piper.exe" yolu).
        /// Boşsa sunucuda ses üretilmez. Program metni standart girdiden alır, sesi bir WAV dosyasına yazar.
        /// </summary>
        public string TtsExecutable { get; set; } = string.Empty;

        /// <summary>Amerikan sesi (.onnx model dosyasının tam yolu).</summary>
        public string TtsModel { get; set; } = string.Empty;

        /// <summary>İsteğe bağlı İngiliz sesi (.onnx). Boşsa İngiliz aksanı için ses üretilmez.</summary>
        public string TtsModelGb { get; set; } = string.Empty;

        /// <summary>Program argümanları. {model} ve {output} yer tutucuları doldurulur.</summary>
        public string TtsArguments { get; set; } = "-m {model} -f {output}";
    }
}
