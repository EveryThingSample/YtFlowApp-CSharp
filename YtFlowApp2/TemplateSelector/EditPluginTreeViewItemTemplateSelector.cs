using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using YtFlowApp2.Models;

namespace YtFlowApp2.TemplateSelector
{
    public class EditPluginTreeViewItemTemplateSelector : DataTemplateSelector
    {
        private DataTemplate m_categoryTemplate;
        private DataTemplate m_pluginTemplate;

        // Constructor
        public EditPluginTreeViewItemTemplateSelector()
        {
        }

        // CategoryTemplate property
        public DataTemplate CategoryTemplate
        {
            get { return m_categoryTemplate; }
            set { m_categoryTemplate = value; }
        }

        // PluginTemplate property
        public DataTemplate PluginTemplate
        {
            get { return m_pluginTemplate; }
            set { m_pluginTemplate = value; }
        }

        // Core SelectTemplate method (without container parameter)
        protected override DataTemplate SelectTemplateCore(object item)
        {
            return Select(item);
        }

        // Core SelectTemplate method (with container parameter)
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return Select(item);
        }

        // Select method to determine which template to use
        private DataTemplate Select(object item)
        {
            // We assume item is of type TreeViewNode and it contains an EditPluginModel instance
            if (item is Microsoft.UI.Xaml.Controls.TreeViewNode tvc && tvc.Content is EditPluginModel)
            {
                return m_pluginTemplate;
            }

            return m_categoryTemplate;
        }
    }
}
