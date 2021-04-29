using System;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BLOCKTools
{
    /// <summary>
    /// Логика работы UI.xaml
    /// </summary>
    public partial class Ui : Window
    {
        private readonly Document _doc;

        //private readonly UIApplication _uiApp;
        private readonly Autodesk.Revit.ApplicationServices.Application _app;
        private readonly UIDocument _uiDoc;

        private readonly EventHandlerWithStringArg _mExternalMethodStringArg;
        private readonly EventHandlerWithWpfArg _mExternalMethodWpfArg;
        private BTSettings settings;

        public Ui(UIApplication uiApp, EventHandlerWithStringArg evExternalMethodStringArg,
            EventHandlerWithWpfArg eExternalMethodWpfArg)
        {
            _uiDoc = uiApp.ActiveUIDocument;
            _doc = _uiDoc.Document;
            _app = _doc.Application;
            //_uiApp = _doc.Application;
            Closed += MainWindow_Closed;


            InitializeComponent();

            // инициализируем переменную с экзмепляром настроек
            settings = App.ThisApp.Settings;

            // Читаем настройки и применяем их к GUI
            ReadSettings();


            _mExternalMethodStringArg = evExternalMethodStringArg;
            _mExternalMethodWpfArg = eExternalMethodWpfArg;
        }


        private void MainWindow_Closed(object sender, EventArgs e)
        {
            App.ThisApp.UiForm = null;
            Close();
        }

        #region External Project Methods

        private void BExtString_Click(object sender, RoutedEventArgs e)
        {
            // Raise external event with a string argument. The string MAY
            // be pulled from a Revit API context because this is an external event
            _mExternalMethodStringArg.Raise($"Title: {_doc.Title}");
        }

        private void BExternalMethod1_Click(object sender, RoutedEventArgs e)
        {
            // Raise external event with this UI instance (WPF) as an argument
            _mExternalMethodWpfArg.Raise(this);
        }

        #endregion

        #region Non-External Project Methods

        private void UserAlert()
        {
            //TaskDialog.Show("Non-External Method", "Non-External Method Executed Successfully");
            MessageBox.Show("Non-External Method Executed Successfully", "Non-External Method");

            //Dispatcher.Invoke(() =>
            //{
            //    TaskDialog mainDialog = new TaskDialog("Non-External Method")
            //    {
            //        MainInstruction = "Hello, Revit!",
            //        MainContent = "Non-External Method Executed Successfully",
            //        CommonButtons = TaskDialogCommonButtons.Ok,
            //        FooterText = "<a href=\"http://usa.autodesk.com/adsk/servlet/index?siteID=123112&id=2484975 \">"
            //                     + "Click here for the Revit API Developer Center</a>"
            //    };


            //    TaskDialogResult tResult = mainDialog.Show();
            //    Debug.WriteLine(tResult.ToString());
            //});
        }

        private void BNonExternal3_Click(object sender, RoutedEventArgs e)
        {
            // the sheet takeoff + delete method won't work here because it's not in a valid Revit api context
            // and we need to do a transaction
            // Methods.SheetRename(this, _doc); <- WON'T WORK HERE
            UserAlert();
        }

        #endregion

        private void cbIsChecked()
        {
            // Обновляем значения полей, отслеживающих состояния выбора чек-боксов
            settings.VertFabInAClass = CBFabVertClA.IsChecked.GetValueOrDefault();
            settings.VertFabInBClass = CBFabVertClB.IsChecked.GetValueOrDefault();
            settings.VertFabInCClass = CBFabVertClC.IsChecked.GetValueOrDefault();
            settings.VertFabInDClass = CBFabVertClD.IsChecked.GetValueOrDefault();
            settings.VertFabInCNCClass = CBFabVertClCNC.IsChecked.GetValueOrDefault();
        }


        public void ReadSettings()
        {
            
            CBFabVertClA.IsChecked = settings.VertFabInAClass;
            CBFabVertClB.IsChecked = settings.VertFabInBClass;
            CBFabVertClC.IsChecked = settings.VertFabInCClass;
            CBFabVertClD.IsChecked = settings.VertFabInDClass;
            CBFabVertClCNC.IsChecked = settings.VertFabInCNCClass;
        }


        /// <summary>
        /// После нажатие кнопки "Применить" применяет текущие настройки на влкадке настроек фабионов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApplyFab_Click(object sender, RoutedEventArgs e)
        {
            // Обновляем состояние полей, отслеживающих нажатие чек-боксов
            cbIsChecked();

            Utils.SerializeSettings();
        }
    }
}