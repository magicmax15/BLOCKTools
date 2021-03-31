using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;


namespace BLOCKTools
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class Lab1PlaceGroup : IExternalCommand
    {
        /// <summary>
        /// Возвращает центр ограничивающей рамки элемента
        /// </summary>
        public XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);

            XYZ center = (bounding.Max + bounding.Min) * 0.5;

            return center;
        }

        ///<summary>
        /// Возвращает комнату, в которой находится точка
        ///</summary>
        Room GetRoomOfGroup(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            Room room = null;
            foreach (Element elem in collector)
            {
                room = elem as Room;
                if (room != null)
                {
                    // Точка в указанной комнате?
                    if (room.IsPointInRoom(point))
                    {
                        break;
                    }
                }
            }
            return room;
        }

        /// <summary>
        /// Возвращает координаты центра комнаты.
        /// Координата Z - уровень пола комнаты
        /// </summary>
        public XYZ GetRoomCenter(Room room)
        {
            // Получение центра комнаты
            XYZ boundCenter = GetElementCenter(room);
            LocationPoint locPt = (LocationPoint)room.Location;
            XYZ roomCenter = new XYZ(boundCenter.X, boundCenter.Y, locPt.Point.Z);
            return roomCenter;
        }

        /// <summary>
        /// Группа скопируется во все выбранные комнаты. Расположение
        /// копий группы отсчитывается от центральных точек комнат;
        /// смещение копий от центров комнат - такое же, 
        /// как у оригинала
        /// </summary>
        public void PlaceFurnitureInRooms(
            Document doc,
            IList<Reference> rooms,
            XYZ sourceCenter,
            GroupType gt,
            XYZ groupOrigin)
        {
            XYZ offset = groupOrigin - sourceCenter;
            XYZ offsetXY = new XYZ(offset.X, offset.Y, 0);

            foreach (Reference r in rooms)
            {
                Room roomTarget = doc.GetElement(r) as Room;
                if (roomTarget != null)
                {
                    XYZ roomCenter = GetRoomCenter(roomTarget);
                    Group group = doc.Create.PlaceGroup(roomCenter + offsetXY, gt);
                }
            }
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            // Получение элементов приложения и документа
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;
            try
            {
                // Указание группы
                Selection sel = uiApp.ActiveUIDocument.Selection;
                // Определение объекта-ссылки для занесения результата указания
                GroupPickFilter selFilter = new GroupPickFilter();
                Reference pickedRef = sel.PickObject(ObjectType.Element, selFilter, "Выберите группу");
                Element elem = doc.GetElement(pickedRef);
                Group group = elem as Group;

                // Получение центра группы
                XYZ origin = GetElementCenter(group);

                // Получение комнаты, в которой находится указанная группа
                Room room = GetRoomOfGroup(doc, origin);

                // Получение центра комнаты
                XYZ sourceCenter = GetRoomCenter(room);

                /*
                string coords =
                    "X = " + sourceCenter.X.ToString() + "\r\n" +
                    "Y = " + sourceCenter.Y.ToString() + "\r\n" +
                    "Z = " + sourceCenter.Z.ToString();
                TaskDialog.Show("Центр исходной комнаты", coords);
                */

                // Предложение выбрать комнаты для копирования группы
                RoomPickFilter roomPickFilter = new RoomPickFilter();
                IList<Reference> rooms = sel.PickObjects(
                    ObjectType.Element,
                    roomPickFilter,
                    "Выберите комнаты, в которые требуется скопировать группу мебели");



                //Указание точки (не используется)
                //XYZ point = sel.PickPoint("Укажите точку для размещения группы");

                // Размещение группы
                Transaction trans = new Transaction(doc);
                trans.Start("Lab");
                PlaceFurnitureInRooms(doc, rooms, sourceCenter, group.GroupType, origin);
                /*
                doc.Create.PlaceGroup(point, group.GroupType);
                // Расчет положени новой группы
                XYZ groupLocation = sourceCenter + new XYZ(13.12, 0, 0);
                doc.Create.PlaceGroup(groupLocation, group.GroupType);
                */
                trans.Commit();
            }
            // Обработка исключений при щелчке правой кнопкой или нажатии ESC
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            // Обработка других ошибок
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }

    /// <summary>
    /// Фильтр, ограничивающий выбор группами модели. Только они
    /// выделяются и могут быть выбраны при наведении курсора.
    /// </summary>
    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return (elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_IOSModelGroups));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    /// <summary>
    ///  Фильтр, ограничивающий выбор только комнатами
    /// </summary>
    public class RoomPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return (elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_Rooms));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}


