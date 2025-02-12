using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YtFlowApp2.Pages.PluginEditor;

namespace YtFlowApp2.Models
{

    public class EditPluginModel : INotifyPropertyChanged
    {
        // 事件实现，用于属性变化通知
        public event PropertyChangedEventHandler PropertyChanged;

        // 私有字段
        private PluginModel _plugin;
        private bool _isEntry;
        private bool _isDirty;
        private bool _hasNamingConflict;
        private IPluginEditorParam _editorParam;

        // 构造函数
        public EditPluginModel() { }

        public EditPluginModel(PluginModel plugin, bool isEntry)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _isEntry = isEntry;
        }

        // 属性
        public PluginModel Plugin => _plugin;

        public bool IsEntry
        {
            get => _isEntry;
            set
            {
                if (_isEntry != value)
                {
                    _isEntry = value;
                    OnPropertyChanged(nameof(IsEntry));
                    OnPropertyChanged(nameof(IsNotEntry));
                }
            }
        }

        public bool IsNotEntry => !_isEntry;

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged(nameof(IsDirty));
                }
            }
        }

        public bool HasNamingConflict
        {
            get => _hasNamingConflict;
            set
            {
                if (_hasNamingConflict != value)
                {
                    _hasNamingConflict = value;
                    OnPropertyChanged(nameof(HasNamingConflict));
                }
            }
        }

        public IPluginEditorParam EditorParam
        {
            get => _editorParam;
            set
            {
                if (_editorParam != value)
                {
                    _editorParam = value;
                    OnPropertyChanged(nameof(EditorParam));
                }
            }
        }

        // 用于触发 PropertyChanged 事件
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 帮助方法：可以调用 PropertyChanged 事件
        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
