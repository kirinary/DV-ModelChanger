using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModelChanger
{
    public class Log
    {
        public static void Write(string message)
        {
            Debug.Log($"[AddHeadMark]{message}");
        }
    }
}
