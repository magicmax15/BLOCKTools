using System;
using Autodesk.Revit.UI;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Threading;
using Autodesk.Revit.UI.Events;

namespace BLOCKTools
{
    class App : IExternalApplication
    {
        public static App ThisApp;

        private Ui uiForm;

        // Отдельный поток для запуска UI на нем
        private Thread uiThread;

        public Result OnStartup(UIControlledApplication app)
        {
            uiForm = null; // диалоговое окно должно появляться только после вызова команды
            ThisApp = this; // статический доступ к экземпляру приложения

            var assembly = Assembly.GetExecutingAssembly();

            string thisAssemblyPath = assembly.Location;
            string assemblyDir = new FileInfo(assembly.Location).DirectoryName;

            //PushButtonData placeFabionBTNData = CreatePlaceFabionBtn(assembly);

            // Создаем новую панель на вкладке Надстройки 
            var panel = app.CreateRibbonPanel(0, "BLOCK Tools");

            // Добавляем выпадающую кнопку на панель
            AddPulldownButtonGroup(panel, assembly);
            AddSettingsPushBtn(panel, assembly);

            // listeners/watchers for external events (if you choose to use them)
            app.ApplicationClosing += a_ApplicationClosing; //Set Application to Idling
            app.Idling += a_Idling;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }

        /// <summary>
        /// Создание выпадающей группы команд
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="assemblyDir"></param>
        private void AddPulldownButtonGroup(RibbonPanel panel, Assembly assembly)
        {
            string thisAssemblyPath = assembly.Location;
            string assemblyDir = new FileInfo(assembly.Location).DirectoryName;
            // Выпадающая кнопка
            PulldownButtonData blockToolsGroupData = new PulldownButtonData("BLOCKTools", "BLOCK Tools");
            blockToolsGroupData.LargeImage = new BitmapImage(new Uri(Path.Combine(assemblyDir, "Resources/Icon1.ico")));
            PulldownButton blockToolsGroup = panel.AddItem(blockToolsGroupData) as PulldownButton;


            // Кнопка "Разместить фабионы"
            PushButtonData placeFabionsData = new PushButtonData("PlaceFabionsBtn", "Фабионы", assembly.Location, "BLOCKTools.PlaceFabion");
            placeFabionsData.LongDescription = "Размещение вертикальных фабионов в пространстве модели в помещениях с классом чистоты, размещение " +
                "горизонтальных фабионов и угловых вставок в параметрах помещений";
            placeFabionsData.ToolTip = "Размещение фабионов и угловых вставок";
            PushButton placeFabionsBtn = blockToolsGroup.AddPushButton(placeFabionsData) as PushButton;
            placeFabionsBtn.LargeImage = new BitmapImage(new Uri(Path.Combine(assemblyDir, "Resources/fabion_16x16.ico")));

            // Кнопка "Разместить все потолки"
            PushButtonData placeAllCeilingsData = new PushButtonData("PlaceAllCeilingsBtn", "Потолки", assembly.Location, "BLOCKTools.PlaceAllCeilings");
            placeAllCeilingsData.LongDescription = "Автоматическое размещение потолков в помещениях с классом чистоты на высоту согласно положению верхней границы помещения";
            placeAllCeilingsData.ToolTip = "Размещение потолков в помещениях с классом чистоты";
            PushButton placeAllCeilingsBtn = blockToolsGroup.AddPushButton(placeAllCeilingsData) as PushButton;
            placeAllCeilingsBtn.LargeImage = new BitmapImage(new Uri(Path.Combine(assemblyDir, "Resources/ceiling_16x16.ico")));
        }

        /// <summary>
        /// Cоздание кнопки настроек
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="assemblyDir"></param>
        private void AddSettingsPushBtn(RibbonPanel panel, Assembly assembly)
        {
            string thisAssemblyPath = assembly.Location;

            if (panel.AddItem(
                new PushButtonData("Settings", "Settings", assembly.Location,
                    "BLOCKTools.EntryCommand")) is PushButton settingsBtn)
            {
                // defines the tooltip displayed when the button is hovered over in Revit's ribbon
                settingsBtn.ToolTip = "Visual interface for debugging applications.";
                // defines the icon for the button in Revit's ribbon - note the string formatting
                Uri uriImage = new Uri("pack://application:,,,/BLOCKTools;component/Resources/code-small.png");
                BitmapImage largeImage = new BitmapImage(uriImage);
                settingsBtn.LargeImage = largeImage;
            }
        }

        /// <summary>
        /// Запускает отображение окна формы WPF, и внедряет любые методы, которые обернуты 
        /// </summary>
        /// <param name="uiapp"></param>
        public void ShowForm(UIApplication uiapp)
        {
            // Если форма еще не создана, то создадим её
            //if (uiForm != null && uiForm == null) return;
            //Внешние события с аргументами
            EventHandlerWithStringArg evStr = new EventHandlerWithStringArg();
            EventHandlerWithWpfArg evWpf = new EventHandlerWithWpfArg();

            // The dialog becomes the owner responsible for disposing the objects given to it.
            uiForm = new Ui(uiapp, evStr, evWpf);
            uiForm.Show();
        }

        /// <summary>
        /// This is the method which launches the WPF window in a separate thread, and injects any methods that are
        /// wrapped by ExternalEventHandlers. This can be done in a number of different ways, and
        /// implementation will differ based on how the WPF is set up.
        /// </summary>
        /// <param name="uiapp">The Revit UIApplication within the add-in will operate.</param>
        public void ShowFormSeparateThread(UIApplication uiapp)
        {
            // If we do not have a thread started or has been terminated start a new one
            if (!(uiThread is null) && uiThread.IsAlive) return;
            //EXTERNAL EVENTS WITH ARGUMENTS
            EventHandlerWithStringArg evStr = new EventHandlerWithStringArg();
            EventHandlerWithWpfArg evWpf = new EventHandlerWithWpfArg();

            uiThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(
                        Dispatcher.CurrentDispatcher));
                // The dialog becomes the owner responsible for disposing the objects given to it.
                uiForm = new Ui(uiapp, evStr, evWpf);
                uiForm.Closed += (s, e) => Dispatcher.CurrentDispatcher.InvokeShutdown();
                uiForm.Show();
                Dispatcher.Run();
            });
        }

        #region Idling & Closing

        /// <summary>
        /// What to do when the application is idling. (Ideally nothing)
        /// </summary>
        void a_Idling(object sender, IdlingEventArgs e)
        {
        }

        /// <summary>
        /// What to do when the application is closing.)
        /// </summary>
        void a_ApplicationClosing(object sender, ApplicationClosingEventArgs e)
        {
        }

        #endregion
    }


}

