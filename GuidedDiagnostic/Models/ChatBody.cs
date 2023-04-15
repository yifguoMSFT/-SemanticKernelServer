namespace GuidedDiagnostic.Models
{
    public class ChatBody
    {
        public Model Model { get; set; }
        public Message[] Messages { get; set; }
        public string Key { get; set; }
        public string Prompt { get; set; }
        public double Temperature { get; set; }
    }
}
