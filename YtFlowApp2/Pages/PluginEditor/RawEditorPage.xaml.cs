using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YtFlowApp2.CoreInterop;
using YtFlowApp2.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YtFlowApp2.Pages.PluginEditor
{
    public interface IPluginEditorParam
    {
        byte[] ToCbor();
        string[] CheckErrors();
    }
    public class RawEditorParam: IPluginEditorParam
    {
        private string m_rawJson;

        public RawEditorParam() { }

        public RawEditorParam(byte[] cbor)
        {
            var obj = CoreInterop.BridgeExtensions.FromCBORBytesToJson(cbor);
            try
            {
                var jsonObj = JObject.Parse(obj);
                EscapeCborBuf(jsonObj);
                m_rawJson = jsonObj.ToString(Formatting.Indented);
            }
            catch
            {
                m_rawJson = obj;
            }
        }

        public string RawJson { get => m_rawJson; set { m_rawJson = value; } }



        public byte[] ToCbor()
        {
            var jsonObj = JObject.Parse(m_rawJson);
            UnescapeCborBuf(jsonObj);
            var cborObj = CBORObject.FromJSONString(jsonObj.ToString());
            return cborObj.EncodeToBytes();
        }

        public string[] CheckErrors()
        {
            try
            {
                var jsonObj = JObject.Parse(m_rawJson);
            }
            catch (JsonReaderException e)
            {
                return new string[] { e.Message };
            }
            return new string[] { };
        }

        public void Prettify()
        {
            var jsonObj = JObject.Parse(m_rawJson);
            m_rawJson = jsonObj.ToString(Formatting.Indented);
        }

        private void EscapeCborBuf(JToken doc)
        {
            if (doc.Type == JTokenType.Array)
            {
                foreach (var item in doc)
                {
                    EscapeCborBuf(item);
                }
            }
            else if (doc.Type == JTokenType.Object)
            {
                foreach (var property in ((JObject)doc).Properties())
                {
                    EscapeCborBuf(property.Value);
                }
            }
            else if (doc.Type == JTokenType.Bytes)
            {
                var bin = (byte[])doc;

                try
                {
                    // Check if the byte array is valid UTF-8
                    var str = Encoding.UTF8.GetString(bin);
                    var jsonObj = new JObject
                    {
                        ["__byte_repr"] = "utf8",
                        ["data"] = str
                    };
                    doc.Replace(jsonObj);
                }
                catch
                {
                    // If not valid UTF-8, encode as Base64
                    var base64 = Convert.ToBase64String(bin);
                    var jsonObj = new JObject
                    {
                        ["__byte_repr"] = "base64",
                        ["data"] = base64
                    };
                    doc.Replace(jsonObj);
                }
            }
        }

        internal static void UnescapeCborBuf(JToken doc)
        {
            if (doc.Type == JTokenType.Array)
            {
                foreach (var item in doc)
                {
                    UnescapeCborBuf(item);
                }
            }
            else if (doc.Type == JTokenType.Object)
            {
                var jsonObj = (JObject)doc;
                if (jsonObj.ContainsKey("__byte_repr") && jsonObj.ContainsKey("data"))
                {
                    var repr = jsonObj["__byte_repr"].ToString();
                    var data = jsonObj["data"].ToString();

                    if (repr == "utf8")
                    {
                        var bytes = Encoding.UTF8.GetBytes(data);
                        doc.Replace(JToken.FromObject(bytes));
                    }
                    else if (repr == "base64")
                    {
                        var bytes = Convert.FromBase64String(data);
                        doc.Replace(JToken.FromObject(bytes));
                    }
                }
                else
                {
                    foreach (var property in jsonObj.Properties())
                    {
                        UnescapeCborBuf(property.Value);
                    }
                }
            }
        }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RawEditorPage : Page
    {
        private const byte PARAM_EDIT_TEXT_INITED = 0b01;
        private const byte PARAM_EDIT_TEXT_STORED = 0b10;

        private EditPluginModel m_model;
        private byte m_paramEditTextChangedStage;

        public RawEditorPage()
        {
            this.InitializeComponent();
        }

        public EditPluginModel Model
        {
            get => m_model;
            set => m_model = value;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var model = e.Parameter as EditPluginModel;
            var editorParam = model.EditorParam as RawEditorParam;

            if (editorParam == null)
            {
                var param = model.Plugin.Param;
                editorParam = new RawEditorParam(param.ToArray());
                model.EditorParam = editorParam;
            }

            Model = model;
            this.Bindings.Update();
            bool isDirty = model.IsDirty;

            model.IsDirty = isDirty;

            PluginTypeText.NavigateUri = new Uri($"https://ytflow.github.io/ytflow-book/plugins/{model.Plugin.Plugin}.html");
            m_paramEditTextChangedStage = 0;
            ParamEdit.Document.SetText(TextSetOptions.None, editorParam.RawJson);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var editorParam = m_model.EditorParam as RawEditorParam;
            ParamEdit.Document.GetText(TextGetOptions.NoHidden, out string text);
            editorParam.RawJson = text;
        }

        public SolidColorBrush PluginNameColor(bool hasNamingConflict)
        {
            return hasNamingConflict
                ? new SolidColorBrush(Windows.UI.Colors.Red)
                : Application.Current.Resources["DefaultTextForegroundThemeBrush"] as SolidColorBrush;
        }

        private void ParamEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((m_paramEditTextChangedStage & PARAM_EDIT_TEXT_STORED) != 0)
            {
                return;
            }

            ParamEdit.Document.GetText(TextGetOptions.NoHidden, out string text);
            (m_model.EditorParam as RawEditorParam).RawJson = text;
            m_paramEditTextChangedStage |= PARAM_EDIT_TEXT_STORED;
        }

        private void ParamEdit_TextChanged(object sender, RoutedEventArgs e)
        {
            if ((m_paramEditTextChangedStage & PARAM_EDIT_TEXT_INITED) != 0)
            {
                m_model.IsDirty = true;
                var q = ~PARAM_EDIT_TEXT_STORED;
    
                m_paramEditTextChangedStage &= (byte)q;
            }
            else
            {
                m_paramEditTextChangedStage |= PARAM_EDIT_TEXT_INITED | PARAM_EDIT_TEXT_STORED;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var pluginModel = m_model.Plugin as PluginModel;
            var original = pluginModel.OriginalPlugin;

            pluginModel.Name = original.name;
            pluginModel.Desc = original.desc;
            pluginModel.Param = original.param.ToArray();

            var param = new RawEditorParam(original.param.ToArray());
            m_model.EditorParam = param;
            m_paramEditTextChangedStage = 0;
            ParamEdit.Document.SetText(TextSetOptions.None, param.RawJson);

            m_model.IsDirty = false;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_model.HasNamingConflict)
            {
                PluginNameBox.Focus(FocusState.Programmatic);
                return;
            }

            var editorParam = m_model.EditorParam as RawEditorParam;
            ParamEdit.Document.GetText(TextGetOptions.NoHidden, out string text);
            editorParam.RawJson = text;

            var paramErrors = editorParam.CheckErrors();
            if (paramErrors.Any())
            {
                ValidateErrorText.Text = string.Join("\r\n", paramErrors);
                ValidateErrorFlyout.ShowAt(SaveButton);
                return;
            }

            editorParam.Prettify();
            ParamEdit.Document.SetText(TextSetOptions.None, editorParam.RawJson);

            var pluginModel = m_model.Plugin as PluginModel;
            var cbor = editorParam.ToCbor();
            pluginModel.Param = cbor;

            try
            {
                pluginModel.Verify();
            }
            catch (Exception ex)
            {
                ValidateErrorText.Text = ex.Message;
                ValidateErrorFlyout.ShowAt(SaveButton);
                return;
            }
    
            var original = pluginModel.OriginalPlugin;
            original.name = pluginModel.Name;
            original.desc = pluginModel.Desc;
            original.param = cbor.ToArray();
            pluginModel.OriginalPlugin = original;

            await Task.Run(() => pluginModel.Update());
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                m_model.IsDirty = false;
            });
        }
    }
}