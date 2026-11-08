namespace WordWise.Application.Common.Settings
{
    /// <summary>Telaffuz ses dosyaları (appsettings: "WordAudio").</summary>
    public class WordAudioSettings
    {
        public const string SectionName = "WordAudio";

        /// <summary>Ses dosyalarının klasörü. Boşsa proje klasöründeki Data/wiktionary-audios aranır.</summary>
        public string Folder { get; set; } = string.Empty;
    }
}
