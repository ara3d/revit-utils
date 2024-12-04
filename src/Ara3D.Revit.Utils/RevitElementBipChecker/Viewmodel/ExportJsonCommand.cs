using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using RevitElementBipChecker.Model;

namespace RevitElementBipChecker.Viewmodel
{
    public class ExportJsonCommand : ICommand
    {
        public BipCheckerViewmodel Viewmodel;
        public ExportJsonCommand(BipCheckerViewmodel vm)
        {
            Viewmodel = vm;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }
        public void Execute(object parameter)
        {

            var parameterDatas = Viewmodel.frmmain.lsBipChecker.Items.Cast<ParameterData>().ToList();
            var dataTable = parameterDatas.ToDataTable2();
            dataTable.Columns.RemoveAt(0);
            dataTable.WriteJson(out var path);
            Process.Start("explorer.exe", path);

        }
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
