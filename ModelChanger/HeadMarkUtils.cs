using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModelChanger
{
    public class HeadMarkUtils
    {
        public static void CreateHeadMark(TrainCar train)
        {
            Log.Write("creating HeadMark");
            GameObject obj;
            var name = "headmark";

            DestoryIfExist(name);

            obj = GameObject.Instantiate(Main.assets["SH282_HeadMark"], train.transform.position, train.transform.rotation, train.transform);
            Log.Write("Instantiate");
            if(obj == null)
            {
                Log.Write("Faild to Instantiate");
                return;
            }
            obj.name = name;
            obj.transform.position += train.transform.forward * 1.71f;
            obj.transform.position += train.transform.up * 0.78f;
            Log.Write("End creating HeadMark");
        }

        public static void DestoryIfExist(string name)
        {
            var gameObject = GameObject.Find(name);
            if (gameObject == null)
            {
                return;
            }
            GameObject.Destroy(gameObject);
        }
    }
}
