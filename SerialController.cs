using System;
using System.IO.Ports;
using UnityEngine;

public class SerialController : MonoBehaviour
{
    public static SerialController Instance { get; private set; }

    private const byte PacketHeader = 255;
    private const int AxisNeutral = 127;
    private const int AxisDeadzone = 20;

    public string portName = "COM3";
    public int baudRate = 115200;

    private SerialPort serialPort;

    private enum PacketState
    {
        WaitingHeader,
        ReadingX,
        ReadingY,
        ReadingButtons
    }

    private PacketState packetState = PacketState.WaitingHeader;
    private DateTime lastPacketTime = DateTime.MinValue;

    // Variables compartidas para gameplay
    public static volatile int Gear = 0;        // 0=Neutral, 1=Drive1, 2=Drive2
    public static volatile int AxisX = AxisNeutral;
    public static volatile int AxisY = AxisNeutral;
    public static volatile int PauseToggleRequested = 0;  // Contador, no booleano
    public static volatile bool HasHardwareData = false;

    private bool previousButton1State = false;
    private bool previousButton2State = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 50;

        try
        {
            string[] availablePorts = SerialPort.GetPortNames();
            serialPort.NewLine = "\n";
            Debug.Log("Puertos detectados: " + string.Join(", ", availablePorts));

            bool portExists = false;
            for (int i = 0; i < availablePorts.Length; i++)
            {
                if (availablePorts[i] == portName)
                {
                    portExists = true;
                    break;
                }
            }

            if (!portExists)
            {
                Debug.LogWarning("Puerto " + portName + " no encontrado. Se usara teclado como fallback.");
                return;
            }

            serialPort.Open();
            HasHardwareData = false;
            Debug.Log("Puerto Serial Abierto Correctamente");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Serial no disponible, se usara teclado como fallback: " + e.Message);
        }
    }

    private void Update()
    {
        if (serialPort == null || !serialPort.IsOpen)
        {
            return;
        }

        try
        {
            while (serialPort.BytesToRead > 0)
            {
                int rawData = serialPort.ReadByte();
                ProcessReceivedData((byte)rawData);
            }
        }
        catch (TimeoutException)
        {
        }
        catch (Exception e)
        {
            Debug.LogError("Error leyendo serial: " + e.Message);
        }

        if (HasHardwareData && (DateTime.UtcNow - lastPacketTime).TotalMilliseconds > 250)
        {
            HasHardwareData = false;
            AxisX = AxisNeutral;
            AxisY = AxisNeutral;
            previousButton1State = false;
            previousButton2State = false;
        }
    }

    private void ProcessReceivedData(byte data)
    {
        switch (packetState)
        {
            case PacketState.WaitingHeader:
                if (data == PacketHeader)
                {
                    packetState = PacketState.ReadingX;
                }
                break;
            case PacketState.ReadingX:
                AxisX = data;
                packetState = PacketState.ReadingY;
                break;
            case PacketState.ReadingY:
                AxisY = data;
                packetState = PacketState.ReadingButtons;
                break;
            case PacketState.ReadingButtons:
                bool button1Pressed = (data & 0x01) != 0;
                bool button2Pressed = (data & 0x02) != 0;

                if (button1Pressed && !previousButton1State)
                {
                    Gear = (Gear + 1) % 3;
                }

                if (button2Pressed && !previousButton2State)
                {
                    PauseToggleRequested++;
                    Debug.Log("Pause button detected! PauseToggleRequested = " + PauseToggleRequested);
                }

                previousButton1State = button1Pressed;
                previousButton2State = button2Pressed;
                HasHardwareData = true;
                lastPacketTime = DateTime.UtcNow;
                packetState = PacketState.WaitingHeader;
                break;
        }
    }

    public void SendRemainingGrains(int remaining)
    {
        if (serialPort == null || !serialPort.IsOpen)
        {
            return;
        }

        int clampedGrains = Mathf.Clamp(remaining, 0, 99);
        string grainText = clampedGrains.ToString("D2");

        try
        {
            serialPort.WriteLine(grainText);
            Debug.Log("Enviando al FPGA: " + grainText + " (Granos: " + clampedGrains + ")");
        }
        catch (Exception e)
        {
            Debug.LogError("Error enviando datos al FPGA: " + e.Message);
        }
    }

    void OnDisable()
    {
        CloseSerial();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            CloseSerial();
            Instance = null;
        }
    }

    void OnApplicationQuit()
    {
        CloseSerial();
    }

    private void CloseSerial()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }

        HasHardwareData = false;
    }
}
