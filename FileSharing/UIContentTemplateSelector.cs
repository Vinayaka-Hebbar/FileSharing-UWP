using FileSharing.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FileSharing
{
    public sealed class UIContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FileTemplate { get; set; }

        public DataTemplate ContentTemplate { get; set; }

        public DataTemplate CodeTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ContentType contentType)
            {
                switch (contentType)
                {
                    case ContentType.File:
                        return FileTemplate;
                    case ContentType.Code:
                        return CodeTemplate;
                    case ContentType.Text:
                    default:
                        return ContentTemplate;
                }
            }
            return base.SelectTemplateCore(item, container);
        }
    }
}
