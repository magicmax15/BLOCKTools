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

    class PlaceFabion : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Получение элементов приложения и документа
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            string typeName = "BFC_102.82_FB_S Hlinikovy fabion";
            FamilySymbol famSym = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Where(q => q.Name == typeName).First() as FamilySymbol;
            
            List<Element> fabions = Utils.GetAllFamilyTypeInstances(doc, typeName);

            try
            {
                List<Room> cleanRooms = Utils.GetAllCleanRooms(doc);
                IDictionary cornersOrientations = new Dictionary<Room, IList<string>>();

                // Удалим все ранее созданные фабионы, если они были размещены в проекте
                if (fabions.Count != 0)
                {
                    Transaction trans = new Transaction(doc);
                    trans.Start("Удаление фабиона");

                    Utils.DeleteElements(doc, fabions);

                    trans.Commit();
                }

                string s = null;

                if (cleanRooms != null)
                {
                    foreach (Room r in cleanRooms)
                    {
                        s += r.Name + " " + r.LookupParameter("BL_Класс чистоты").AsString() + "\n";

                        // Определение границ помещений
                        List<Line> roomBoundary = Utils.GetRoomBoundary(r);

                        // Определение ориентации углов помещений
                        List<string> roomCornerOrient = Utils.GetRoomCornerOrientation(roomBoundary);

                        // Определение коорректирующих углов поворота для размещения фабионов
                        List<int> fabionRotations = Utils.RoomAnglesOrientations(roomCornerOrient, roomBoundary);

                        // Определение координат внутренних углов помещения
                        List<Point> startPoints = Utils.GetInnerCornerList(roomBoundary, roomCornerOrient);

                        // Базовый уровень помещения
                        Level level = r.Level;

                        for (int i = 0; i < startPoints.Count; i++)
                        {
                            Transaction trans = new Transaction(doc);
                            trans.Start("Place fabion");
                            XYZ location = startPoints[i].Coord;
                            Line axis = Line.CreateBound(location, new XYZ(location.X, location.Y, location.Z + 10));
                            FamilyInstance fabion = doc.Create.NewFamilyInstance(location, famSym, level, StructuralType.Column);
                            ElementTransformUtils.RotateElement(doc, fabion.Id, axis, fabionRotations[i] * Math.PI / 180);
                            trans.Commit();
                        }
                    }

                    TaskDialog.Show("Чистые помещения", s);
                }


            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }
}