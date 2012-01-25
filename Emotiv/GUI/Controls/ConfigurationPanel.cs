using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using MCAEmotiv.Classification;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// This custom control dynamically structures itself to allow configuration of the public parameters of 
    /// a specified type of object. The control can be manipulated by setting the configured object, and
    /// a configured object parametrized with the user's input can be retrieved. 
    /// </summary>
    public class ConfigurationPanel : GroupBox
    {
        #region ---- Event ----
        /// <summary>
        /// Contains information relating to the property changed event
        /// </summary>
        public class PropertyChangedEventArgs : EventArgs
        {
            /// <summary>
            /// The property whose value changed
            /// </summary>
            public PropertyInfo Property { get; private set; }

            /// <summary>
            /// A function which retrieves the current value of the property
            /// </summary>
            public Func<object> Getter { get; private set; }

            /// <summary>
            /// A function which sets the current value of the property
            /// </summary>
            public Action<object> Setter { get; private set; }

            /// <summary>
            /// The source of this event
            /// </summary>
            public ConfigurationPanel ConfigPanel { get; private set; }

            /// <summary>
            /// If this event was triggered by the property change of an inner ConfigurationPanel,
            /// this property contains the original event args
            /// </summary>
            public PropertyChangedEventArgs InnerArgs { get; private set; }

            /// <summary>
            /// Construct an event
            /// </summary>
            public PropertyChangedEventArgs(ConfigurationPanel configPanel,
                PropertyInfo property,
                PropertyChangedEventArgs innerArgs = null)
            {
                this.ConfigPanel = configPanel;
                this.Property = property;
                this.Getter = configPanel.getters[property];
                this.Setter = configPanel.setters[property];
                this.InnerArgs = innerArgs;
            }
        }

        /// <summary>
        /// Handles the property changed event
        /// </summary>
        public delegate void PropertyChangedEventHandler(PropertyChangedEventArgs args);
        
        /// <summary>
        /// This event is fired whenever a property of the configured object represented by this
        /// control changes in value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedSafe(PropertyInfo property, PropertyChangedEventArgs innerArgs = null)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(new PropertyChangedEventArgs(this, property, innerArgs));
        }
        #endregion

        private readonly object source;
        /// <summary>
        /// The type of the configurable object represented by this control
        /// </summary>
        public Type SourceType { get { return this.source.GetType(); } }

        private readonly Dictionary<PropertyInfo, Func<object>> getters = new Dictionary<PropertyInfo, Func<object>>();
        private readonly Dictionary<PropertyInfo, Action<object>> setters = new Dictionary<PropertyInfo, Action<object>>();
        private readonly ToolTip tooltip = new ToolTip();

        /// <summary>
        /// Constructs a control from the specified source object
        /// </summary>
        public ConfigurationPanel(object source)
            : base()
        {
            this.source = source;
            this.BuildView();
        }

        /// <summary>
        /// Retrieves the object represented by the control
        /// </summary>
        public object GetConfiguredObject()
        {
            object obj = source.GetFactory()();
            foreach (var pair in getters)
                obj.SetProperty(pair.Key, pair.Value());

            return obj;
        }

        /// <summary>
        /// Sets the control to reflect the properties of the argument, which 
        /// must be of the source type
        /// </summary>
        public void SetConfiguredObject(object obj)
        {
            if (!this.SourceType.IsAssignableFrom(obj.GetType()))
                throw new Exception("Object must be of source type");

            foreach (var param in obj.GetParameters())
                if (this.setters.ContainsKey(param.Property))
                    this.setters[param.Property](obj.GetProperty(param.Property));
        }

        /// <summary>
        /// Disposes of the control
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.tooltip.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Retrieves a getter function for the specified property
        /// </summary>
        public Func<object> GetterFor(PropertyInfo property)
        {
            return this.getters[property];
        }

        /// <summary>
        /// Retrieves a setter function for the specified property
        /// </summary>
        public Action<object> SetterFor(PropertyInfo property)
        {
            return this.setters[property];
        }

        /// <summary>
        /// Creates a control, using the generic argument to specify the source type
        /// </summary>
        public static ConfigurationPanel Create<T>(DockStyle dockStyle = DockStyle.Fill)
            where T : new()
        {
            return new ConfigurationPanel(new T()) { Dock = dockStyle };
        }

        #region ---- View Construction ----
        private void BuildView()
        {
            this.SuspendLayout();
            this.AutoSize = true;
            this.Dock = DockStyle.Fill;
            this.Text = this.SourceType.DisplayName();
            this.tooltip.SetToolTip(this, this.SourceType.Description());

            var panel = new Panel() { Dock = DockStyle.Fill, AutoSize = true, AutoScroll = true };

            // fill the getters dictionary
            Control control;
            Label label;
            string tooltipText;
            foreach (var param in this.source.GetParameters().Reverse())
            {
                control = this.BuildControl(param, out label);
                tooltipText = GetTooltipText(param);

                // add tooltip unless the control has its own tooltip
                if (!(control is ConfigurationPanel || control is DerivedTypeConfigurationPanel))
                    this.tooltip.SetToolTip(control, tooltipText);

                panel.Controls.Add(control);

                // add label
                if (label != null)
                {
                    this.tooltip.SetToolTip(label, tooltipText);
                    panel.Controls.Add(label);
                }
            }

            panel.ReverseTabOrder();
            this.Controls.Add(panel);
            this.ResumeLayout(false);
        }

        private static string GetTooltipText(ParameterAttribute param)
        {
            var sb = new StringBuilder(param.Description);

            if (param.Property.PropertyType == typeof(double))
                sb.AppendLine().Append("Type: real number");
            else if (param.Property.PropertyType == typeof(int))
                sb.AppendLine().Append("Type: integer");
            else if (param.Property.PropertyType.IsEnum)
            {
                sb.AppendLine();
                foreach (var desc in Enum.GetValues(param.Property.PropertyType).Cast<Enum>().Select(e => e.GetDescriptionForEnum()))
                    sb.AppendLine().Append(desc.DisplayName + ": " + desc.Description);
            }

            if (param.HasMinValue())
                sb.Append(Environment.NewLine).Append("Min value: ").Append(param.MinValue);
            if (param.HasMaxValue())
                sb.Append(Environment.NewLine).Append("Max value: ").Append(param.MaxValue);

            return sb.ToString();
        }

        private Control BuildControl(ParameterAttribute param, out Label label)
        {
            label = null;
            Control control;
            if (param.Property.PropertyType == typeof(bool))
                return this.BuildBoolControl(param);
            else if (param.Property.PropertyType == typeof(double))
            {
                label = param.DisplayName.ToLabel();
                return this.BuildDoubleControl(param);
            }
            else if (param.Property.PropertyType == typeof(int))
            {
                label = param.DisplayName.ToLabel();
                return this.BuildIntControl(param);
            }
            else if (param.Property.PropertyType == typeof(string))
            {
                label = param.DisplayName.ToLabel();
                return this.BuildStringControl(param);
            }
            else if (param.Property.PropertyType.IsEnum)
            {
                label = param.DisplayName.ToLabel();
                return this.BuildEnumControl(param);
            }
            else if (param.Property.PropertyType.IsInterface || param.Property.PropertyType.IsAbstract)
            {
                control = new DerivedTypeConfigurationPanel(param, this.source.GetProperty(param.Property)) { Dock = DockStyle.Top };
                this.getters[param.Property] = ((DerivedTypeConfigurationPanel)control).GetConfiguredObject;
                this.setters[param.Property] = ((DerivedTypeConfigurationPanel)control).SetConfiguredObject;
            }
            else if (!param.Property.PropertyType.GetParametersForType().IsEmpty())
            {
                control = new ConfigurationPanel(this.source.GetProperty(param.Property)) { Dock = DockStyle.Top };
                this.getters[param.Property] = ((ConfigurationPanel)control).GetConfiguredObject;
                this.setters[param.Property] = ((ConfigurationPanel)control).SetConfiguredObject;
                ((ConfigurationPanel)control).PropertyChanged += (args) => this.RaisePropertyChangedSafe(param.Property, args);
            }
            else
                throw new Exception("Property type " + param.Property.PropertyType + " is not supported");

            return control;
        }

        private TextBox BuildStringControl(ParameterAttribute param)
        {
            var tb = new TextBox()
            {
                Dock = DockStyle.Top,
                Text = (string)(this.source.GetProperty(param.Property)
                    ?? (param.HasDefaultValue()
                        ? param.DefaultValue
                        : string.Empty))
            };

            this.getters[param.Property] = () => tb.Text;
            this.setters[param.Property] = (o) => tb.Text = o.ToString();
            tb.TextChanged += (sender, args) => this.RaisePropertyChangedSafe(param.Property);
            return tb;
        }

        private TextBox BuildDoubleControl(ParameterAttribute param)
        {
            var tb = new TextBox()
            {
                Dock = DockStyle.Top,
                Text = param.GetClosestValueInBounds((double)this.source.GetProperty(param.Property)).ToString()
            };

            this.getters[param.Property] = () => double.Parse(tb.Text);
            this.setters[param.Property] = (o) => tb.Text = ((double)o).ToString();

            tb.EnableValidation(s =>
            {
                double val;
                return double.TryParse(s, out val)
                    && !(param.HasMinValue() && val < (double)param.MinValue)
                    && !(param.HasMaxValue() && val > (double)param.MaxValue);
            },
            () => this.RaisePropertyChangedSafe(param.Property));

            return tb;
        }

        private TextBox BuildIntControl(ParameterAttribute param)
        {
            var tb = new TextBox()
            {
                Dock = DockStyle.Top,
                Text = param.GetClosestValueInBounds((int)this.source.GetProperty(param.Property)).ToString()
            };

            this.getters[param.Property] = () => int.Parse(tb.Text);
            this.setters[param.Property] = (o) => tb.Text = ((int)o).ToString();

            tb.EnableValidation(s =>
            {
                int val;
                return int.TryParse(s, out val)
                    && !(param.HasMinValue() && val < (int)param.MinValue)
                    && !(param.HasMaxValue() && val > (int)param.MaxValue);
            },
            () => this.RaisePropertyChangedSafe(param.Property));

            return tb;
        }

        private CheckBox BuildBoolControl(ParameterAttribute param)
        {
            var cb = new CheckBox() { Dock = DockStyle.Top, Checked = (bool)this.source.GetProperty(param.Property), Text = param.DisplayName };

            this.getters[param.Property] = () => cb.Checked;
            this.setters[param.Property] = (o) => cb.Checked = (bool)o;
            cb.CheckedChanged += (sender, args) => this.RaisePropertyChangedSafe(param.Property);
            return cb;
        }

        private ComboBox BuildEnumControl(ParameterAttribute param)
        {
            var dropDown = new ComboBox() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            dropDown.MouseWheel += (sender, args) => ((HandledMouseEventArgs)args).Handled = true;
            var map = new Dictionary<Enum, int>();
            int i = 0;
            foreach (Enum value in Enum.GetValues(param.Property.PropertyType))
            {
                dropDown.Items.Add(new DisplayPointer(value.GetDescriptionForEnum().DisplayName, value));
                map[value] = i++;
            }
            dropDown.SelectedIndex = 0;

            this.getters[param.Property] = () => ((DisplayPointer)dropDown.SelectedItem).Key;
            this.setters[param.Property] = (o) => dropDown.SelectedIndex = map[(Enum)o];
            dropDown.SelectedIndexChanged += (sender, args) => this.RaisePropertyChangedSafe(param.Property);
            return dropDown;
        }
        #endregion
    }
}
