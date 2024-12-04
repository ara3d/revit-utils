using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using RevitElementBipChecker.Model;

namespace RevitElementBipChecker.Viewmodel
{
    public class CopyCommand : ICommand
    {
        public BipCheckerViewmodel vm;
        public CopyCommand(BipCheckerViewmodel vm)
        {
            this.vm = vm;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var name = parameter as string;
            switch (name)
            {
                case "BuildIn":
                    Copy_BuiltInParameter();
                    break;
                case "PraName":
                    Copy_ParameterName();
                    break;
                case "Type":
                    Copy_Type();
                    break;
                case "Value":
                    Copy_Value();
                    break;
                case "PraGroup":
                    Copy_ParameterGroup();
                    break;
                case "GName":
                    Copy_GroupName();
                    break;
                case "GUID":
                    Copy_Guid();
                    break;
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        ParameterData GetSelectedItem()
        {
            var selected = vm.frmmain.lsBipChecker.SelectedIndex;
            return  vm.frmmain.lsBipChecker.Items[selected] as ParameterData;
        }
        private void Copy_BuiltInParameter()
        {
            var parameterData = GetSelectedItem();
            Clipboard.SetText(parameterData.BuiltInParameter);
        }
        private void Copy_ParameterName()
        {
            var parameterData = GetSelectedItem();
            Clipboard.SetText(parameterData.ParameterName);
        }
        private void Copy_Type()
        {
            var parameterData = GetSelectedItem();
            Clipboard.SetText(parameterData.Type);
        }
        private void Copy_Value()
        {
            var parameterData = GetSelectedItem();
            Clipboard.SetText(parameterData.Value);
        }
        private void Copy_ParameterGroup()
        {
            var parameterData = GetSelectedItem();
            Clipboard.SetText(parameterData.ParameterGroup);
        }
        private void Copy_GroupName()
        {
            var parameterData = GetSelectedItem();
            Clipboard.SetText(parameterData.GroupName);
        }
        private void Copy_Guid()
        {
            var parameterData = GetSelectedItem();
            Clipboard.SetText(parameterData.GUID);
        }
    }
}
