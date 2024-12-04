using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitElementBipChecker.Model;
using RevitElementBipChecker.View;
using RevitElementBipChecker.Viewmodel;

namespace RevitElementBipChecker.Command
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.UsingCommandData)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var vm = new BipCheckerViewmodel(uidoc);
            var frMainWindows = new MainWindows(vm);
            frMainWindows.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            frMainWindows.SetRevitAsWindowOwner();
            frMainWindows.Show();
            return Result.Succeeded;
        }
    }
}
