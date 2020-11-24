using Newtonsoft.Json;
using Prism.Mvvm;

namespace WakeUp.Model
{
    public class ProgramModel : BindableBase
    {
        private string _name;
        [JsonProperty("app_name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private byte[] _icon;
        [JsonProperty("app_logo")]
        public byte[] Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }
    }
}
