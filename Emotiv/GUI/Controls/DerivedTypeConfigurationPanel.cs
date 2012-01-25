using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// As ConfigurationPanel, but for an abstract or interface type. The control allows
    /// one of that type's derived types to be chosen at which point a ConfigurationPanel for
    /// the derived type is presented.
    /// </summary>
    public class DerivedTypeConfigurationPanel : GroupBox
    {
        private readonly Type baseType;
        private readonly DescriptionAttribute description;
        private readonly Func<object> getter;
        private readonly Action<object> setter;

        #region ---- Constructors ----
        private DerivedTypeConfigurationPanel(Type baseType, DescriptionAttribute description, object source)
            : base()
        {
            if (!baseType.IsAssignableFrom(source.GetType()))
                throw new Exception("source must derive from baseType");

            this.baseType = baseType;
            this.description = description;
            this.BuildView(source, out this.getter, out this.setter);
        }

        /// <summary>
        /// Construct a control from the specified abstract type and source object, which must
        /// be of the given type. The types description is used.
        /// </summary>
        public DerivedTypeConfigurationPanel(Type sourceType, object source)
            : this(sourceType, sourceType.GetDescriptionForType(), source)
        {
        }

        /// <summary>
        /// Construct a control using the specified attribute to provide the source type
        /// and description. The source object must be of the parameter's property type.
        /// </summary>
        public DerivedTypeConfigurationPanel(ParameterAttribute param, object source)
            : this(param.Property.PropertyType, param, source)
        {
        }
        #endregion

        /// <summary>
        /// Retrieves the configured object represented by the control
        /// </summary>
        public object GetConfiguredObject()
        {
            return this.getter();
        }

        /// <summary>
        /// Sets the control to represent obj, which must be of the control's type
        /// </summary>
        public void SetConfiguredObject(object obj)
        {
            if (!this.baseType.IsAssignableFrom(obj.GetType()))
                throw new Exception("object must derive from " + this.baseType.FullName);
            this.setter(obj);
        }

        /// <summary>
        /// Creates a control using the generic argument to specify the type
        /// </summary>
        public static DerivedTypeConfigurationPanel Create<T>(DockStyle dockStyle = DockStyle.Fill)
        {
            return new DerivedTypeConfigurationPanel(typeof(T), typeof(T).GetImplementingTypes().First().New()) { Dock = dockStyle };
        }

        #region ---- View Construction ----
        private void BuildView(object source, out Func<object> getter, out Action<object> setter)
        {
            this.SuspendLayout();
            this.AutoSize = true;
            this.Dock = DockStyle.Fill;
            this.Text = this.description.DisplayName;
            
            var panel = new Panel() { Dock = DockStyle.Fill, AutoSize = true, AutoScroll = true };
            var configs = this.baseType
                .GetImplementingTypes()
                .Where(c => c.GetConstructor(Utils.EmptyArgs) != null)
                .Select(c =>
                {
                    object instance = c.IsAssignableFrom(source.GetType()) ? source : c.New();
                    var configPanel = new ConfigurationPanel(instance) { Dock = DockStyle.Top, Text = string.Empty };
                    return new
                    {
                        Panel = configPanel,
                        Show = !instance.GetParameters().IsEmpty()
                    };
                })
                .ToArray();
            panel.Controls.AddRange(configs.Select(c => c.Panel).ToArray());

            var dropDown = new ComboBox() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            dropDown.MouseWheel += (sender, args) => ((HandledMouseEventArgs)args).Handled = true;
            dropDown.Items.AddRange(configs.Select(c => c.Panel.SourceType.DisplayName()).ToArray());
            dropDown.SelectedIndexChanged += (sender, args) =>
            {
                for (int i = 0; i < configs.Length; i++)
                    configs[i].Panel.Visible = (i == dropDown.SelectedIndex && configs[i].Show);
            };
            dropDown.SelectedIndex = 0;
            panel.Controls.Add(dropDown);

            this.Controls.Add(panel);

            getter = () => configs[dropDown.SelectedIndex].Panel.GetConfiguredObject();
            setter = (o) =>
            {
                var config = configs.Where(c => c.Panel.SourceType.IsAssignableFrom(o.GetType())).First();
                config.Panel.SetConfiguredObject(o);
                dropDown.SelectedItem = config.Panel.SourceType.DisplayName();
            };
            setter(source);

            this.ResumeLayout(false);
        }
        #endregion
    }
}
