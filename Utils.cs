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
using System.Threading;
using System.Diagnostics;
using System.Xml.Serialization;
using System.IO;

namespace BLOCKTools
{
    static class Utils
    {
        static string CLEAN_CLASS_SPARAM_GUID_STR = "02f7e9d4-f055-479e-a36f-5b8e098d40f5";

        static Guid CLEAN_CLASS_SPARAM_GUID = new Guid(CLEAN_CLASS_SPARAM_GUID_STR);
        /// <summary>
        /// Возвращает список всех чистых помещений
        /// </summary>
        public static List<Room> GetAllCleanRooms(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            RoomFilter roomFilter = new RoomFilter();
            collector.WherePasses(roomFilter);

            // Настройка фильтра для комнат заданным параметром "BL_Класс чистоты",
            // равным "A", "B", "C" или "D"
            Room fRoom = collector.FirstElement() as Room;

            Parameter testSharedParam = fRoom.get_Parameter(CLEAN_CLASS_SPARAM_GUID);
            ElementId sharedParamId = testSharedParam.Id;
            ParameterValueProvider provider = new ParameterValueProvider(sharedParamId);
            FilterStringRuleEvaluator fsreEqual = new FilterStringEquals();

            string testStrCNC = "CNC";
            string testStrK = "K";
            string testStrKrus = "К";
            string testVoidStrCNC = "";
            FilterRule fCNCRule = new FilterStringRule(provider, fsreEqual, testStrCNC, false);
            FilterRule fVoidStrRule = new FilterStringRule(provider, fsreEqual, testVoidStrCNC, false);
            FilterRule fKRule = new FilterStringRule(provider, fsreEqual, testStrK, false);
            FilterRule fKrusRule = new FilterStringRule(provider, fsreEqual, testStrKrus, false);

            ElementParameterFilter paramNoCNCFilter = new ElementParameterFilter(fCNCRule, true);
            ElementParameterFilter paramNoVoidFilter = new ElementParameterFilter(fVoidStrRule, true);
            ElementParameterFilter paramNoKFilter = new ElementParameterFilter(fKRule, true);
            ElementParameterFilter paramNoKrusFilter = new ElementParameterFilter(fKrusRule, true);

            collector.WherePasses(paramNoVoidFilter).WherePasses(paramNoCNCFilter).WherePasses(paramNoKFilter).WherePasses(paramNoKrusFilter);

            List<Room> cleanRooms = (from e in collector.ToElements()
                                     where e is Room
                                     select e as Room).ToList<Room>();

            return cleanRooms;
        }

        /// <summary>
        /// Возвращает список всех помещений
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static List<Room> GetAllRoomList(Document doc)
        {
            FilteredElementCollector collector = GetAllRoomCollector(doc);
            List<Room> rooms = (from e in collector.ToElements()
                                     where e is Room
                                     select e as Room).ToList<Room>();

            return rooms;
        }

        /// <summary>
        /// Возвращает FilteredElementCollector из всех помещений в модели
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static FilteredElementCollector GetAllRoomCollector(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            RoomFilter roomFilter = new RoomFilter();
            collector.WherePasses(roomFilter);
            return collector;
        }

        /// <summary>
        /// Возвращает коллектор из всех помещений с заполненным параметром "BL_Класс чистоты"
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static FilteredElementCollector GetAllCleanRoomCollector (Document doc)
        {
            // GUID параметра "BL_Класс чистоты"

            List<Room> rooms = GetAllRoomList(doc);
            ParameterValueProvider provider = GetSharedParameterProvider(CLEAN_CLASS_SPARAM_GUID_STR, rooms[0]);
            FilterStringRuleEvaluator fsreEqual = new FilterStringEquals();
            FilteredElementCollector collector = GetAllRoomCollector(doc);
            // Убрать из списка помещения без заполеннного класса чистоты
            string testVoidStrCNC = "";
            FilterRule fVoidStrRule = new FilterStringRule(provider, fsreEqual, testVoidStrCNC, false);
            ElementParameterFilter paramNoVoidFilter = new ElementParameterFilter(fVoidStrRule, true);
            collector.WherePasses(paramNoVoidFilter);
            return collector;
        }

        public static ParameterValueProvider GetSharedParameterProvider(String guidStr, Element elem)
        {
            Guid guid = new Guid(guidStr);
            Parameter testSharedParam = elem.get_Parameter(guid);
            ElementId sharedParamId = testSharedParam.Id;
            return new ParameterValueProvider(sharedParamId);
        }

        /// <summary>
        /// Фильтрует список помещений по заданному правилу
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="spGuid"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static List<Room> RoomListFilter(Document doc, FilterRule rule)
        {
            List<Room> rooms = GetAllRoomList(doc);
            List<ElementId> roomsID = (from r in rooms select r.Id).ToList<ElementId>(); 
            ElementParameterFilter filter = new ElementParameterFilter(rule, true);
 
            return rooms;
        }



        /// <summary>
        /// Возвращает список комнат для установки в них вертикальных фабионов согласно настройкам программы
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static List<Room> GetAllRoomWithVertFab (Document doc)
        {
            // Получаем доступ к экземпляру класса настроек
            BTSettings settings = App.ThisApp.Settings;

            // Получаем коллекцию всех помещений с заполеннным классом чистоты
            FilteredElementCollector collector = GetAllCleanRoomCollector(doc);

            ParameterValueProvider provider = GetSharedParameterProvider(
                CLEAN_CLASS_SPARAM_GUID_STR, 
                collector.FirstElement());

            Dictionary<string, bool> markedClass = new Dictionary<string, bool>
            {
                {"A", settings.VertFabInAClass},
                {"B", settings.VertFabInBClass},
                {"C", settings.VertFabInCClass},
                {"D", settings.VertFabInDClass},
                {"CNC", settings.VertFabInCNCClass},
                {"K", settings.VertFabInCNCClass},
                {"К", settings.VertFabInCNCClass}
            };

            string testStr = null;
            foreach (string s in markedClass.Keys)
            {
                if (markedClass[s] == false)
                {
                    testStr = s;
                    FilterRule fStrRule = new FilterStringRule(provider, new FilterStringEquals(), testStr, false);
                    ElementParameterFilter fStrRuleFilter = new ElementParameterFilter(fStrRule, true);
                    collector.WherePasses(fStrRuleFilter);
                }
            }

            return (from e in collector.ToElements()
                    select e as Room).ToList<Room>();
        }

        /***
        public static List<Room> GetAllRoomWithHorFab (Document doc)
        {
            
            
            return;
        }
        ***/

        /// <summary>
        /// Возвращает список из Curve, составляющих границу помещения
        /// </summary>
        public static List<Line> GetRoomBoundary(Room room)
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
        public static List<XYZ> VectorDirection(IList<Line> boundary)
        {
            List<XYZ> direction = (from l in boundary
                                   select l.Direction).ToList();

            return direction;
        }

        /// <summary>
        /// Возвращает список с метками ориетации углов(двух смежных линий в контуре комнаты) "ВНЕШ", "ВНУТ" или "ПАРР"
        /// </summary>
        public static List<string> GetRoomCornerOrientation(List<Line> roomBoundary)
        {

            string GetCornerOrientation(XYZ vc)
            {
                int vcZ = (int)vc.Z;
                string sVecCr;
                if (vcZ > 0)
                {
                    sVecCr = "ВНУТ";
                }
                else if (vcZ < 0)
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

            List<XYZ> normalizedVectors = Utils.VectorDirection(roomBoundary);

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
        /// Возвращает количество внутренних углов комнаты
        /// </summary>
        /// <param name="cornerList">список типов углов комнаты</param>
        /// <returns></returns>
        public static int GetInnerCornerCount(List<string> cornerList)
        {
            return (from c in cornerList where c.Equals("ВНУТ") select c).Count();
        }

        /// <summary>
        /// Возвращает количество внешних углов
        /// </summary>
        /// <param name="cornerList">список типов углов комнаты</param>
        /// <returns></returns>
        public static int GetOutterCornerCount(List<string> cornerList)
        {
            return (from c in cornerList where c.Equals("ВНЕШ") select c).Count();
        }

        /// <summary>
        /// Вовзращает угол между вектором v и базовым ветором [0, -1, 0] 
        /// </summary>
        public static int VectorOrintationAngle(XYZ v)
        {
            int bx = 0;
            int by = -1;
            int bz = 0;
            int vx = (int)v.X;
            int vy = (int)v.Y;
            int vz = (int)v.Z;

            int angleInRad = (int)(Math.Acos((vx * bx + vy * by + vz * bz) /
                (Math.Sqrt(Math.Pow(vx, 2) + Math.Pow(vy, 2) + Math.Pow(vz, 2)) *
                Math.Sqrt(Math.Pow(bx, 2) + Math.Pow(by, 2) + Math.Pow(bz, 2)))) * 180 / Math.PI);

            return angleInRad;
        }

        /// <summary>
        /// Возвращает список углов поворота для позиционирования фабионов во внутренних углах помещения
        /// </summary>
        public static List<int> RoomAnglesOrientations(IList<string> cornerOrient, IList<Line> boundary)
        {
            List<XYZ> normalizedVectors = Utils.VectorDirection(boundary);

            List<int> angles = (from v in normalizedVectors
                                let angle = Utils.VectorOrintationAngle(v)
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
        public static List<Point> GetInnerCornerList(List<Line> boundary, List<string> orientations)
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

        /// <summary>
        /// Находит все экземпляры указанного типа
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static List<Element> GetAllFamilyTypeInstances(Document doc, string typeName)
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
        /// Удаляет в текущем документе указанный список элементов
        /// </summary>
        public static void DeleteElements(Document document, List<Element> elements)
        {
            // Удаляет все элементы через список элементов
            ICollection<ElementId> elementsId = (from e in elements select e.Id).ToList();

            ICollection<ElementId> deletedIdSet = document.Delete(elementsId);
        }

        public static void ClearHorFabions(Document doc)
        {

        }

        public static void LogThreadInfo(string name = "")
        {
            Thread th = Thread.CurrentThread;
            Debug.WriteLine($"Task Thread ID: {th.ManagedThreadId}, Thread Name: {th.Name}, Process Name: {name}");
        }

        public static void HandleError(Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Debug.WriteLine(ex.Source);
            Debug.WriteLine(ex.StackTrace);
        }

        /// <summary>
        /// Экспортирует текущее состояние класса BTSettings (настройки) в файл .xml
        /// </summary>
        public static void SerializeSettings()
        {
            XmlSerializer formatter = new XmlSerializer(typeof(BTSettings));

            // получаем поток, куда будем записывать сериализованный объект
            using (FileStream fs = new FileStream("D:/settings.xml", FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, App.ThisApp.Settings);

                Console.WriteLine("Объект сериализован");
            }
        }

        /// <summary>
        /// Импортирует настройки из файла .xml в состояние класса BTSettings
        /// </summary>
        public static void DeserializeSettings()
        {
            XmlSerializer formatter = new XmlSerializer(typeof(BTSettings));

            using (FileStream fs = new FileStream("D:/settings.xml", FileMode.OpenOrCreate))
            {
                App.ThisApp.Settings  = (BTSettings)formatter.Deserialize(fs);
            }

        }

    }
}
