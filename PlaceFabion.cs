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
        /// <summary>
        /// Возвращает список всех чистых помещений
        /// </summary>
        public List<Room> GetAllCleanRooms(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            RoomFilter roomFilter = new RoomFilter();
            collector.WherePasses(roomFilter);

            // Настройка фильтра для комнат заданным параметром "BL_Класс чистоты",
            // равным "A", "B", "C" или "D"
            Guid spGuid = new Guid("02f7e9d4-f055-479e-a36f-5b8e098d40f5");
            Room fRoom = collector.FirstElement() as Room;

            Parameter testSharedParam = fRoom.get_Parameter(spGuid);
            ElementId sharedParamId = testSharedParam.Id;
            ParameterValueProvider provider = new ParameterValueProvider(sharedParamId);
            FilterStringRuleEvaluator fsreEqual = new FilterStringEquals();

            string testStrCNC = "CNC";
            string testVoidStrCNC = "";
            FilterRule fCNCRule = new FilterStringRule(provider, fsreEqual, testStrCNC, false);
            FilterRule fVoidStrRule = new FilterStringRule(provider, fsreEqual, testVoidStrCNC, false);

            ElementParameterFilter paramNoCNCFilter = new ElementParameterFilter(fCNCRule, true);
            ElementParameterFilter paramNoVoidFilter = new ElementParameterFilter(fVoidStrRule, true);

            collector.WherePasses(paramNoVoidFilter).WherePasses(paramNoCNCFilter);

            List<Room> cleanRooms = (from e in collector.ToElements()
                                      where e is Room
                                      select e as Room).ToList<Room>();

            return cleanRooms;
        }

        /// <summary>
        /// Возвращает список из Curve, составляющих границу помещения
        /// </summary>
        public List<Line> GetRoomBoundary(Room room)
        {
            List<Line> roomBoundary = new List<Line>();
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
            opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;
            opt.StoreFreeBoundaryFaces = true;
            IList<IList<BoundarySegment>> segments = room.GetBoundarySegments(opt);

           if (null != segments)
           {
               foreach (IList<BoundarySegment> segmentList in segments)
               {
                   foreach (BoundarySegment boundarySegment in segmentList)
                   {
                       roomBoundary.Add(boundarySegment.GetCurve() as Line);
                   }
               }
           }

           return roomBoundary;
        }

        /// <summary>
        /// Возвращает список нормализованных векторов на основе входного списка объектов типа Line, составляющих границы помещения
        /// </summary>
        public List<XYZ> VectorDirection(IList<Line> boundary)
        {
            List<XYZ> direction = (from l in boundary
                                    select l.Direction).ToList();

            return direction;
        }

        /// <summary>
        /// Возвращает список с метками ориетации углов(двух смежных линий в контуре комнаты) "ВНЕШ", "ВНУТ" или "ПАРР"
        /// </summary>
        public List<string> GetRoomCornerOrientation(List<Line> roomBoundary)
        {   

            string GetCornerOrientation(XYZ vc)
            {
                int vcZ = (int) vc.Z;
                string sVecCr;
                if (vcZ > 0)
                {
                    sVecCr = "ВНУТ";
                } 
                else if(vcZ < 0)
                {
                    sVecCr = "ВНЕШ";
                }
                else
                {
                    sVecCr = "ПАРР";
                }

                return sVecCr;
            }

            /*
            string sbound = null;
            for (int i = 0; i < roomBoundary.Count; i++)
            {
                sbound += roomBoundary[i].Direction.ToString() + "\n";
            }
            TaskDialog.Show("Направление линий границы", sbound);
            */

            List<XYZ> normalizedVectors = VectorDirection(roomBoundary);

            List<string> cornersOrientations = new List<string>();
            string orientation;

            XYZ v1 = normalizedVectors.Last();
            XYZ v2 = normalizedVectors[0];
            XYZ crossVector = v1.CrossProduct(v2);
            orientation = GetCornerOrientation(crossVector);
            cornersOrientations.Add(orientation);
            
            for (int i = 0; i < normalizedVectors.Count - 1; i++)
            {
                v1 = normalizedVectors[i];
                v2 = normalizedVectors[i + 1];
                crossVector = v1.CrossProduct(v2);
                orientation = GetCornerOrientation(crossVector);
                cornersOrientations.Add(orientation);
                //vcross += crossVector.Z + "\n";
                //s += orientation + "\n";
            }

            //TaskDialog.Show("Ориентация вершин", s);
            //TaskDialog.Show("Векторное произведение", vcross);

            return cornersOrientations;
        }

        /// <summary>
        /// Вовзращает угол между вектором v и базовым ветором [0, -1, 0] 
        /// </summary>
        public int VectorOrintationAngle(XYZ v)
        {
            int bx = 0; 
            int by = -1;    
            int bz = 0;
            int vx = (int)v.X;
            int vy = (int)v.Y;
            int vz = (int)v.Z;

            int angleInRad =  (int)(Math.Acos((vx * bx + vy * by + vz * bz) /
                (Math.Sqrt(Math.Pow(vx, 2) + Math.Pow(vy, 2) + Math.Pow(vz, 2)) *
                Math.Sqrt(Math.Pow(bx, 2) + Math.Pow(by, 2) + Math.Pow(bz, 2)))) * 180 / Math.PI);

            return angleInRad;
        }

        /// <summary>
        /// Возвращает список углов поворота для позиционирования фабионов во внутренних углах помщеения
        /// </summary>
        public List<int> RoomAnglesOrientations(IList<string> cornerOrient, IList<Line> boundary)
        {
            List<XYZ> normalizedVectors = VectorDirection(boundary);

            List<int> angles = (from v in normalizedVectors
                                let angle = VectorOrintationAngle(v)
                                select angle).ToList();

            /*
            string sAngles = null;
            for (int i = 0; i < angles.Count; i++)
            {
                sAngles += angles[i] + "\n";
            }
            TaskDialog.Show("Углы", sAngles);
            */

            int index = angles.IndexOf(0);
            List<int> anglesPart1 = angles.GetRange(0, index);
            List<int> anglesPart2 = angles.GetRange(index, angles.Count - index);

            int rotateAngle = 0;
            List<int> rotates = new List<int>();
            for (int i = 0; i < angles.Count(); i++)
            {
                rotates.Add(0);
            }

            for (int i = 1; i < anglesPart2.Count(); i++)
            {
                if (cornerOrient[index + i] == "ВНУТ")
                {
                    rotateAngle += 90;
                }
                if (cornerOrient[index + i] == "ВНЕШ")
                {
                    rotateAngle -= 90;
                }

                rotates[index + i] = rotateAngle;
            }

            for (int i = 0; i < anglesPart1.Count(); i++)
            {
                if (cornerOrient[i] == "ВНУТ")
                {
                    rotateAngle += 90;
                }
                if (cornerOrient[i] == "ВНЕШ")
                {
                    rotateAngle -= 90;
                }

                rotates[i] = rotateAngle;
            }

            /*
            string sRotates = null;
            foreach (int r in rotates)
            {
                sRotates += r + "\n";
            }
            TaskDialog.Show("Углы поворота", sRotates);
            */

            List<int> filteredInternalRotates = new List<int>();

            //string s = null;

            for (int i = 0; i < cornerOrient.Count(); i++)
            {
                if (cornerOrient[i] == "ВНУТ")
                {
                    filteredInternalRotates.Add(rotates[i]);
                    //s += rotates[i] + "\n";
                }
            }

            //TaskDialog.Show("Углы поворота внутренних углов", s);

            return filteredInternalRotates;
        }

        /// <summary>
        /// Возвращает список координат внутренних углов помещения
        /// </summary>
        public List<Point> GetInnerCornerList(List<Line> boundary, List<string> orientations)
        {
            List<Point> startPointList = new List<Point>();

            for (int i = 0; i < boundary.Count(); i++)
            {
                if (orientations[i] == "ВНУТ")
                {
                    startPointList.Add(Point.Create(boundary[i].GetEndPoint(0)));
                }
            }

            return startPointList;
        }

        public List<Element> GetAllFamilyTypeInstances(Document doc, string typeName)
        {
            // Находит все типоразмеры семейств в текущем документе
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector = collector.OfClass(typeof(FamilySymbol));

            // Находит среди всех типоразмеров типоразмеры с заданным именем "typeName" и определяет Id первого из списка
            var query = from element in collector where element.Name == typeName select element;
            List<Element> famSyms = query.ToList<Element>();
            ElementId symbolId = famSyms[0].Id;

            // Создает FamilyInstance фильтр по найденному FamilySymbol Id
            FamilyInstanceFilter filter = new FamilyInstanceFilter(doc, symbolId);

            // Применяет фильтр к элементам из текущего документа
            collector = new FilteredElementCollector(doc);
            List<Element> familyInstances = collector.WherePasses(filter).ToElements().ToList();

            return familyInstances;
        }

        /// <summary>
        /// Удаляет в текущем документе указанный элемент
        /// </summary>
        public void DeleteElements(Document document, List<Element> elements)
        {
            // Delete all the selected elements via the set of elements
            ICollection<ElementId> elementsId = (from e in elements select e.Id).ToList();

            ICollection<ElementId> deletedIdSet = document.Delete(elementsId);


        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Получение элементов приложения и документа
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            string typeName = "BFC_102.82_FB_S Hlinikovy fabion";
            FamilySymbol famSym = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Where(q => q.Name == typeName).First() as FamilySymbol;
            
            List<Element> fabions = GetAllFamilyTypeInstances(doc, typeName);

            try
            {
                List<Room> cleanRooms = GetAllCleanRooms(doc);
                IDictionary cornersOrientations = new Dictionary<Room, IList<string>>();

                // Удалим все ранее созданные фабионы, если они были размещены в проекте
                if (fabions.Count != 0)
                {
                    Transaction trans = new Transaction(doc);
                    trans.Start("Удаление фабиона");

                    DeleteElements(doc, fabions);

                    trans.Commit();
                }

                string s = null;

                if (cleanRooms != null)

                foreach (Room r in cleanRooms)
                {
                    s += r.Name + " " + r.LookupParameter("BL_Класс чистоты").AsString() + "\n";

                    // Определение границ помещений
                    List<Line> roomBoundary = GetRoomBoundary(r);

                    // Определение ориентации углов помещений
                    List<string> roomCornerOrient = GetRoomCornerOrientation(roomBoundary);

                    // Определение коорректирующих углов поворота для размещения фабионов
                    List<int> fabionRotations = RoomAnglesOrientations(roomCornerOrient, roomBoundary);

                    // Определение координат внутренних углов помещения
                    List<Point> startPoints = GetInnerCornerList(roomBoundary, roomCornerOrient);

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