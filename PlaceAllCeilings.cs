using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Structure;

namespace BLOCKTools
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    class PlaceAllCeilings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Получение элементов приложения и документа
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            return Result.Succeeded;
        }
    }
}
