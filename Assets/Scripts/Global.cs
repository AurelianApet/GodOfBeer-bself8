using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Globalization;
using System.Linq;
using SimpleJSON;
using UnityEngine.SceneManagement;

public struct ProductInfo
{
    public int serial_number;
    public WorkSceneType sceneType;//standby, pour, soldout, remain
    public int standby_type;//image or text method
    public int server_id;
    public int id;
    public int is_soldout;
    public int sell_type;//0-cup, 1-ml
    public int cup_size;
    public int unit_price;
    public string drink_name;
    //public string kind;
    //public int bintage;
    public string country;
    public string styles;
    public string description;
    public int board_no;
    public int board_ch;
    public int gw_no;
    public int gw_ch;
}

public enum WorkSceneType
{
    standby = 1,
    pour,
    remain,
    soldout
}

public class Global
{
    public static int scene_type = 0;//0-autoslider, 1-main
    //image download path
    public static string imgPath = "";
    public static string prePath = "";
    public static ProductInfo[] pInfo = new ProductInfo[8];
    public static string ip = "";
    public static List<string> backImgFiles = new List<string>();
    public static string backVideoFile = "";
    //api
    public static int newStatusBarValue;
    public static string api_server_port = "3006";
    public static string api_url = "";
    public static string check_db_api = "check-db";
    public static string save_setinfo_api = "save-tap-info";
    public static string get_product_api = "get-product";
    public static string bottle_init_confirm_api = "keg-init-confirm";
    public static string cancel_soldout_api = "cancel-soldout";
    public static string get_media_api = "media";
    public static string soldout_api = "soldout";
    public static string image_server_path = "http://" + ip + ":" + api_server_port + "/self/";
    public static string backImageServerPath = "http://" + ip + ":" + api_server_port + "/self/media/";
    public static string socket_server = "";

    public static string GetPriceFormat(float price)
    {
        return string.Format("{0:N0}", price);
    }

    public static void setStatusBarValue(int value)
    {
        newStatusBarValue = value;
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                try
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(setStatusBarValueInThread));
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
        }
    }

    private static void setStatusBarValueInThread()
    {
#if UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                {
                    window.Call("setFlags", newStatusBarValue, -1);
                }
            }
        }
#endif
    }
}