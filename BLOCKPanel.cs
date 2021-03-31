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

            PushButtonData pushButtonData = new PushButtonData(
                "BLOCK_PlaceFabionBtn", 
                "Place Fabion", 
                assembly.Location, 
                "BLOCKTools.PlaceFabion");

            pushButtonData.LongDescription = "Разместить вертикальные фабионы";

            // Ищем путь к файлу изображения
            var assemblyDir = new FileInfo(assembly.Location).DirectoryName;
            var imagePath = Path.Combine(assemblyDir, "Icon1.ico");
            pushButtonData.LargeImage = new BitmapImage(new Uri(imagePath));

            // Создаем новую панель на вкладке Надстройки 
            var panel = app.CreateRibbonPanel(0, "BLOCK Tools");

            // Добавляем кнопку на панель
            panel.AddItem(pushButtonData);

            return Result.Succeeded;
        }
    }
}

