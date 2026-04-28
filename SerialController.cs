using UnityEngine;
using System.IO.Ports;
using System;

public class SerialController : MonoBehaviour
{
    public static SerialController Instance { get; private set; }

    [Header("Configuración del Puerto")]
    public string portName = "COM3"; // ¡Ajusta este puerto!
    public int baudRate = 115200;

    public static bool HasHardwareData { get; private set; } = false;
    public static int Gear { get; private set; } = 0;
    public static float DirectionX { get; private set; } = 0f;
    public static float DirectionY { get; private set; } = 0f;

    private SerialPort serialPort;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitializeSerialPort();
    }

    private void InitializeSerialPort()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 10;
            serialPort.Open();
            HasHardwareData = true;
            Debug.Log("FPGA Conectada. Listo para la presentación.");
        }
        catch (Exception e)
        {
            HasHardwareData = false;
            Debug.LogWarning("FPGA no detectada: " + e.Message);
        }
    }

    private void Update()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                while (serialPort.BytesToRead > 0)
                {
                    int rawData = serialPort.ReadByte();
                    ProcessData(rawData);
                }
            }
            catch (TimeoutException) { }
        }
    }

    private void ProcessData(int data)
    {
        if (data == 80) // ASCII de 'P' (Pausa)
        {
            if (PauseManager.Instance != null) PauseManager.Instance.TogglePause();
        }
        else if (data == 71) // ASCII de 'G' (Marcha)
        {
            Gear++;
            if (Gear > 2) Gear = 0; // N -> D1 -> D2 -> N
        }
        else if (data >= 100 && data <= 115) // Protocolo de Movimiento de los Switches
        {
            int swData = data - 100;


            bool right = (swData & 1) != 0;  // SW(0) - Bit 0 (Valor 1)
            bool left = (swData & 2) != 0;  // SW(1) - Bit 1 (Valor 2)
            bool up = (swData & 4) != 0;  // SW(2) - Bit 2 (Valor 4)
            bool down = (swData & 8) != 0;  // SW(3) - Bit 3 (Valor 8)

            DirectionX = right ? 1f : (left ? -1f : 0f);
            DirectionY = up ? 1f : (down ? -1f : 0f);
        }
    }

    public void SendRemainingGrains(int amount)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                byte[] buffer = { (byte)amount };
                serialPort.Write(buffer, 0, 1);
            }
            catch { }
        }
    }

    private void OnDestroy() { if (serialPort != null && serialPort.IsOpen) serialPort.Close(); }
}
