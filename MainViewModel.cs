using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SexToyScriptViewer
{
    class MainViewModel : INotifyPropertyChanged
    {
        private int _height = 400;
        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                NotifyPropertyChanged(nameof(Height));
            }
        }

        protected void NotifyPropertyChanged([CallerMemberName] string? caller = null)
        {
            PropertyChanged?.Invoke(this, new(caller));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
