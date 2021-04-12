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

            // Добавляем кнопку на панель

            AddPulldownButtonGroup(panel, assembly);

            return Result.Succeeded;
        }

        PushButtonData CreatePlaceFabionBtn(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData(
                                            "BLOCK_PlaceFabionBtn",
                                            "Place Fabions",
                                            assembly.Location,
                                            "BLOCKTools.PlaceFabion");

            pushButtonData.LongDescription = "Разместить фабионы и угловые вставки";

            // Ищем путь к файлу изображения
            var assemblyDir = new FileInfo(assembly.Location).DirectoryName;
            var imagePath = Path.Combine(assemblyDir, "Icon1.ico");
            pushButtonData.LargeImage = new BitmapImage(new Uri(imagePath));
            return pushButtonData;
        }

        // Создание выпадающей группы команд BLOCKTools 
        private void AddPulldownButtonGroup(RibbonPanel panel, Assembly assembly)
        {
            var assemblyDir = new FileInfo(assembly.Location).DirectoryName;
            // Выпадающая кнопка
            PulldownButtonData blockToolsGroupData = new PulldownButtonData("BLOCKTools", "BLOCK Tools");
            blockToolsGroupData.LargeImage = new BitmapImage(new Uri(Path.Combine(assemblyDir, "Icon1.ico")));
            PulldownButton blockToolsGroup = panel.AddItem(blockToolsGroupData) as PulldownButton;
            

            // Кнопка "Разместить фабионы"
            PushButtonData placeFabionsData = new PushButtonData("PlaceFabionsBtn", "Фабионы", assembly.Location, "BLOCKTools.PlaceFabion");
            placeFabionsData.LongDescription = "Размещение вертикальных фабионов в пространстве модели в помещениях с классом чистоты, размещение " +
                "горизонтальных фабионов и угловых вставок в параметрах помещений";
            placeFabionsData.ToolTip = "Размещение фабионов и угловых вставок";
            PushButton placeFabionsBtn = blockToolsGroup.AddPushButton(placeFabionsData) as PushButton;
            placeFabionsBtn.LargeImage = new BitmapImage(new Uri(Path.Combine(assemblyDir, "fabion_16x16.ico")));
        }
    }
}

