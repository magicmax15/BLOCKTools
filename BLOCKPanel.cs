using System;
using Autodesk.Revit.UI;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;


namespace BLOCKTools
{
    class BLOCKPanel : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Cancelled;
        }

        public Result OnStartup(UIControlledApplication app)
        {
            var assembly = Assembly.GetExecutingAssembly();

            //PushButtonData placeFabionBTNData = CreatePlaceFabionBtn(assembly);

            // Создаем новую панель на вкладке Надстройки 
            var panel = app.CreateRibbonPanel(0, "BLOCK Tools");

            // Добавляем выпадающую кнопку на панель
            AddPulldownButtonGroup(panel, assembly);

            return Result.Succeeded;
        }

        // Создание выпадающей группы команд BLOCKTools 
        private void AddPulldownButtonGroup(RibbonPanel panel, Assembly assembly)
        {
            var assemblyDir = new FileInfo(assembly.Location).DirectoryName;
            // Выпадающая кнопка
            PulldownButtonData blockToolsGroupData = new PulldownButtonData("BLOCKTools", "BLOCK Tools");
            blockToolsGroupData.LargeImage = new BitmapImage(new Uri(Path.Combine(assemblyDir, "resources/Icon1.ico")));
            PulldownButton blockToolsGroup = panel.AddItem(blockToolsGroupData) as PulldownButton;
            

            // Кнопка "Разместить фабионы"
            PushButtonData placeFabionsData = new PushButtonData("PlaceFabionsBtn", "Фабионы", assembly.Location, "BLOCKTools.PlaceFabion");
            placeFabionsData.LongDescription = "Размещение вертикальных фабионов в пространстве модели в помещениях с классом чистоты, размещение " +
                "горизонтальных фабионов и угловых вставок в параметрах помещений";
            placeFabionsData.ToolTip = "Размещение фабионов и угловых вставок";
            PushButton placeFabionsBtn = blockToolsGroup.AddPushButton(placeFabionsData) as PushButton;
            placeFabionsBtn.LargeImage = new BitmapImage(new Uri(Path.Combine(assemblyDir, "resources/fabion_16x16.ico")));

            // Кнопка "Разместить все потолки"
            PushButtonData placeAllCeilingsData = new PushButtonData("PlaceAllCeilingsBtn", "Потолки", assembly.Location, "BLOCKTools.PlaceAllCeilings");
            placeAllCeilingsData.LongDescription = "Автоматическое размещение потолков в помещениях с классом чистоты на высоту согласно положению верхней границы помещения";
            placeAllCeilingsData.ToolTip = "Размещение потолков в помещениях с классом чистоты";
            PushButton placeAllCeilingsBtn = blockToolsGroup.AddPushButton(placeAllCeilingsData) as PushButton;
            placeAllCeilingsBtn.LargeImage = new BitmapImage(new Uri(Path.Combine(assemblyDir, "resources/ceiling_16x16.ico")));
        }
    }
}

