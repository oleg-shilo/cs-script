using Aurora;
using Aurora.Devices;

// using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class RPiDeviceScript
{
    //!!!!!!!!!! SCRIPT SETTINGS !!!!!!!!!!//
    public bool enabled = true; //Switch to True, to enable it in Aurora

    private static readonly int NUMBER_OF_LEDS = 410; //Number of LEDs connected to your Raspberry pi
    private static readonly string PI_URL = "http://192.168.0.153:8032/"; //The URL to which requests will be sent to
    private static readonly string PI_URL_SUFFIX_START = "start";
    private static readonly string PI_URL_SUFFIX_STOP = "stop";
    private static readonly string PI_URL_SUFFIX_SETLIGHTS = "lights";
    private static readonly bool WAIT_FOR_RESPONSE = true; //Should this script wait for a response from Raspberry pi

    private static readonly Dictionary<DeviceKeys, int[]> PI_LED_MAPPING = new Dictionary<DeviceKeys, int[]>()
        {
			//{ DeviceKeys.NUM_ZERO, Enumerable.Range(0, 410).ToArray() },
			{ DeviceKeys.C, new int[] {66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329} },
            { DeviceKeys.CLOSE_BRACKET, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65 } },
            { DeviceKeys.OPEN_BRACKET, new int[] { 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 390, 391, 392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409 } }
        };

    public string devicename = "Raspberry Pi Device Script";
    private bool initialized = false;

    private Color[] device_colors;

    private enum ActionCodes
    {
        None = 0,
        Initialize = 1,
        Stop = 2,
        SetLightis = 3
    }

    public bool Initialize()
    {
        try
        {
            if (!initialized)
            {
                //Perform necessary actions to initialize your device
                Reset();

                initialized = SendJsonToRPI(ActionCodes.Initialize) == HttpStatusCode.OK;
            }
            return initialized;
        }
        catch (Exception exc)
        {
            Global.logger.LogLine("[" + devicename + "] Exception: " + exc.ToString());

            return false;
        }
    }

    public void Reset()
    {
        //Perform necessary actions to reset your device
        device_colors = new Color[NUMBER_OF_LEDS];

        for (int col_i = 0; col_i < NUMBER_OF_LEDS; col_i++)
            device_colors[col_i] = Color.FromArgb(0, 0, 0);
    }

    public void Shutdown()
    {
        if (initialized)
        {
            //Perform necessary actions to shutdown your device
            SendJsonToRPI(ActionCodes.Stop);

            initialized = false;
        }
    }

    public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, bool forced)
    {
        try
        {
            //Gather colors
            Color[] newColors = new Color[NUMBER_OF_LEDS];

            foreach (KeyValuePair<DeviceKeys, Color> key in keyColors)
            {
                //Iterate over each key and color and prepare to send them to the device
                if (PI_LED_MAPPING.ContainsKey(key.Key))
                {
                    foreach (int id in PI_LED_MAPPING[key.Key])
                    {
                        newColors[id] = key.Value;
                    }
                }
            }

            System.Threading.Tasks.Task.Run(() => SendColorsToDevice(newColors, forced));

            return true;
        }
        catch (Exception exc)
        {
            Global.logger.LogLine("[" + devicename + "] Exception: " + exc.ToString());

            return false;
        }
    }

    //Custom method to send the color to the device
    private void SendColorsToDevice(Color[] colors, bool forced)
    {
        //Check if device's current color is the same, no need to update if they are the same
        if (!Enumerable.SequenceEqual(colors, device_colors) || forced)
        {
            RPI_Packet packet = new RPI_Packet(colors);

            if (!WAIT_FOR_RESPONSE || SendJsonToRPI(ActionCodes.SetLightis, packet) == HttpStatusCode.OK)
            {
                // Pi responded! Colors must've been set
                device_colors = colors;
            }
        }
    }

    private HttpStatusCode SendJsonToRPI(ActionCodes action, RPI_Packet packet = null)
    {
        if (!initialized && action != ActionCodes.Initialize)
            return HttpStatusCode.NotFound;

        string request_url = PI_URL;

        switch (action)
        {
            case ActionCodes.Initialize:
                request_url += PI_URL_SUFFIX_START;
                break;

            case ActionCodes.Stop:
                request_url += PI_URL_SUFFIX_STOP;
                break;

            case ActionCodes.SetLightis:
                request_url += PI_URL_SUFFIX_SETLIGHTS;
                break;
        }

        var httpWebRequest = (HttpWebRequest)WebRequest.Create(request_url);
        ServicePointManager.DefaultConnectionLimit = 15;
        httpWebRequest.Proxy = null;
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";
        httpWebRequest.KeepAlive = false;

        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
            if (packet != null)
                streamWriter.Write(packet.ToJson());
            streamWriter.Flush();
            streamWriter.Close();
        }

        HttpStatusCode code = HttpStatusCode.NotFound;
        using (HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
        {
            code = httpResponse.StatusCode;

            httpResponse.Close();
        }

        return code;
    }
}

public class RPI_Packet
{
    public int[] col = null;

    public RPI_Packet()
    {
    }

    public RPI_Packet(Color[] colors)
    {
        col = new int[colors.Length];

        for (int i = 0; i < colors.Length; i++)
        {
            Color c = colors[i];

            if (c == null)
                col[i] = 0; //Completely black
            else
                col[i] = (c.R << 16) | (c.G << 8) | (c.B);
        }
    }

    public string ToJson()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("{");

        if (col == null)
            sb.Append("\"col\": null");
        else
        {
            sb.Append("\"col\": [");
            for (int i = 0; i < col.Length; i++)
            {
                sb.Append(col[i]);
                if (i < col.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");
        }
        sb.Append("}");

        return sb.ToString();
    }
}