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
        private UIApplication uiapp;
        private UIDocument uidoc;
        private Application app;
        private Document doc;
        private Transaction trans;

        private List<Element> fabions;
        private List<Room> rooms;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Получение элементов приложения и документа
            uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument;
            app = uiapp.Application;
            doc = uidoc.Document;

            try
            {
                PlaceVertFabs();
                PlaceGorFabs();
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

        public void PlaceVertFabs()
        {
            rooms = Utils.GetAllRoomWithVertFab(doc);
            IDictionary cornersOrientations = new Dictionary<Room, IList<string>>();

            string typeName = "BFC_102.82_FB_S Hlinikovy fabion";
            FamilySymbol famSym = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Where(q => q.Name == typeName).First() as FamilySymbol;

            string komaxitMatName = "BFC_KX9016";
            Material komaxitMat = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().Where(q => q.Name == komaxitMatName).First();

            fabions = Utils.GetAllFamilyTypeInstances(doc, typeName);

            // Удалим все ранее созданные фабионы, если они были размещены в проекте
            if (fabions.Count != 0)
            {
                trans = new Transaction(doc);
                trans.Start("Удаление фабиона");

                Utils.DeleteElements(doc, fabions);

                trans.Commit();
            }

            string s = null;

            if (rooms != null)
            {
                foreach (Room r in rooms)
                {
                    s += r.Name + " " + r.LookupParameter("BL_Класс чистоты").AsString() + "\n";

                    // Определение границ помещений
                    List<Line> roomBoundary = Utils.GetRoomBoundary(r);

                    // Определение ориентации углов помещений
                    List<string> roomCornerOrient = Utils.GetRoomCornerOrientation(roomBoundary);

                    // Количество внутренних углов помещения
                    int innerCornerCount = Utils.GetInnerCornerCount(roomCornerOrient);

                    // Количество внешних углов помещения
                    int outerCornerCount = Utils.GetOutterCornerCount(roomCornerOrient);

                    // Определение коорректирующих углов поворота для размещения фабионов
                    List<int> fabionRotations = Utils.RoomAnglesOrientations(roomCornerOrient, roomBoundary);

                    // Определение координат внутренних углов помещения
                    List<Point> startPoints = Utils.GetInnerCornerList(roomBoundary, roomCornerOrient);

                    // Базовый уровень помещения
                    Level level = r.Level;

                    using (Transaction t = new Transaction(doc, "Place fabions"))
                    {
                        t.Start();

                        // Расстановка фабионов в углах чистых помещений
                        for (int i = 0; i < startPoints.Count; i++)
                        {

                            XYZ location = startPoints[i].Coord;
                            Line axis = Line.CreateBound(location, new XYZ(location.X, location.Y, location.Z + 10));
                            FamilyInstance fabion = doc.Create.NewFamilyInstance(location, famSym, level, StructuralType.Column);
                            ElementTransformUtils.RotateElement(doc, fabion.Id, axis, fabionRotations[i] * Math.PI / 180);

                            // Установить расчет количества "0" - подсчет по длине заказа
                            fabion.LookupParameter("BFC_Pocet_prvku").Set(0);

                            // Установить размер для заказа
                            fabion.LookupParameter("BFC_Rozmer").Set(UnitUtils.ConvertToInternalUnits(3500, UnitTypeId.Millimeters));

                            // Установить высоту фабиона по полной высоте помещения
                            fabion.LookupParameter("BFC_Delka_prvku").Set(r.UnboundedHeight);

                            // Установить смещение снизу для фабиона
                            fabion.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(r.BaseOffset);

                        }
                         
                        t.Commit();
                    }
                }

                TaskDialog.Show("Чистые помещения", s);
            }
        }

        public void PlaceGorFabs()
        {
            rooms = Utils.GetAllRoomWithVertFab(doc);
            IDictionary cornersOrientations = new Dictionary<Room, IList<string>>();

            string typeName = "BFC_102.82_FB_S Hlinikovy fabion";
            FamilySymbol famSym = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Where(q => q.Name == typeName).First() as FamilySymbol;

            string komaxitMatName = "BFC_KX9016";
            Material komaxitMat = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().Where(q => q.Name == komaxitMatName).First();

            fabions = Utils.GetAllFamilyTypeInstances(doc, typeName);

            // Удалим все ранее созданные фабионы, если они были размещены в проекте
            if (fabions.Count != 0)
            {
                trans = new Transaction(doc);
                trans.Start("Удаление фабиона");

                Utils.DeleteElements(doc, fabions);

                trans.Commit();
            }

            string s = null;

            if (rooms != null)
            {
                foreach (Room r in rooms)
                {
                    s += r.Name + " " + r.LookupParameter("BL_Класс чистоты").AsString() + "\n";

                    // Определение границ помещений
                    List<Line> roomBoundary = Utils.GetRoomBoundary(r);

                    // Определение ориентации углов помещений
                    List<string> roomCornerOrient = Utils.GetRoomCornerOrientation(roomBoundary);

                    // Количество внутренних углов помещения
                    int innerCornerCount = Utils.GetInnerCornerCount(roomCornerOrient);

                    // Количество внешних углов помещения
                    int outerCornerCount = Utils.GetOutterCornerCount(roomCornerOrient);

                    // Определение коорректирующих углов поворота для размещения фабионов
                    List<int> fabionRotations = Utils.RoomAnglesOrientations(roomCornerOrient, roomBoundary);

                    // Определение координат внутренних углов помещения
                    List<Point> startPoints = Utils.GetInnerCornerList(roomBoundary, roomCornerOrient);

                    // Базовый уровень помещения
                    Level level = r.Level;

                    using (Transaction t = new Transaction(doc, "Place fabions"))
                    {
                        t.Start();

                        // Расстановка фабионов в углах чистых помещений
                        for (int i = 0; i < startPoints.Count; i++)
                        {

                            XYZ location = startPoints[i].Coord;
                            Line axis = Line.CreateBound(location, new XYZ(location.X, location.Y, location.Z + 10));
                            FamilyInstance fabion = doc.Create.NewFamilyInstance(location, famSym, level, StructuralType.Column);
                            ElementTransformUtils.RotateElement(doc, fabion.Id, axis, fabionRotations[i] * Math.PI / 180);

                            // Установить расчет количества "0" - подсчет по длине заказа
                            fabion.LookupParameter("BFC_Pocet_prvku").Set(0);

                            // Установить размер для заказа
                            fabion.LookupParameter("BFC_Rozmer").Set(UnitUtils.ConvertToInternalUnits(3500, UnitTypeId.Millimeters));

                            // Установить высоту фабиона по полной высоте помещения
                            fabion.LookupParameter("BFC_Delka_prvku").Set(r.UnboundedHeight);

                            // Установить смещение снизу для фабиона
                            fabion.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(r.BaseOffset);

                        }

                        // Заполнение параметра потолочных фабионов
                        Parameter paramDM1 = r.LookupParameter("BFC_DM1_Polozka");
                        paramDM1.Set("FB");
                        Parameter paramDM1pocet = r.LookupParameter("BFC_DM1_Pocet_prvku");
                        paramDM1pocet.Set(0);
                        Parameter paramDM1rozmer = r.LookupParameter("BFC_DM1_Rozmer");
                        paramDM1rozmer.Set(0);
                        Parameter paramDM1material = r.LookupParameter("BFC_DM1_Material");
                        paramDM1material.Set(komaxitMat.Id);

                        // Заполнение параметров угловых внутренних скруглений
                        Parameter paramDM2poloz = r.LookupParameter("BFC_DM2_Polozka");
                        paramDM2poloz.Set("3F");
                        Parameter paramDM2pocet = r.LookupParameter("BFC_DM2_Pocet_prvku");
                        paramDM2pocet.Set(innerCornerCount);
                        Parameter paramDM2rozmer = r.LookupParameter("BFC_DM2_Rozmer");
                        paramDM2rozmer.Set(1);
                        Parameter paramDM2material = r.LookupParameter("BFC_DM2_Material");
                        paramDM2material.Set(komaxitMat.Id);

                        // Заполнение параметров угловных наружных скруглений
                        if (outerCornerCount > 0)
                        {
                            Parameter paramDM3 = r.LookupParameter("BFC_DM3_Polozka");
                            paramDM3.Set("2FV");
                            Parameter paramDM3pocet = r.LookupParameter("BFC_DM3_Pocet_prvku");
                            paramDM3pocet.Set(outerCornerCount);
                            Parameter paramDM3rozmer = r.LookupParameter("BFC_DM3_Rozmer");
                            paramDM3rozmer.Set(1);
                            Parameter paramDM3material = r.LookupParameter("BFC_DM3_Material");
                            paramDM3material.Set(komaxitMat.Id);
                        }

                        t.Commit();
                    }
                }

                TaskDialog.Show("Чистые помещения", s);
            }
        }
    }
}