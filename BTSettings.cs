using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BLOCKTools
{
    [Serializable]
    public class BTSettings
    {
        private static BTSettings instance;

        public BTSettings()
        {

        }

        private bool vertFabInAClass = true;
        private bool vertFabInBClass = true;
        private bool vertFabInCClass = true;
        private bool vertFabInDClass = true;
        private bool vertFabInCNCClass = false;

        public bool VertFabInAClass { get => vertFabInAClass; set => vertFabInAClass = value; }
        public bool VertFabInBClass { get => vertFabInBClass; set => vertFabInBClass = value; }
        public bool VertFabInCClass { get => vertFabInCClass; set => vertFabInCClass = value; }
        public bool VertFabInDClass { get => vertFabInDClass; set => vertFabInDClass = value; }
        public bool VertFabInCNCClass { get => vertFabInCNCClass; set => vertFabInCNCClass = value; }

        public static BTSettings getInstance()
        {
            if (instance == null)
                instance = new BTSettings();
            return instance;
        }

     }
}
