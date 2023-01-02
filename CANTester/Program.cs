using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System;
using System.Threading;
using CTRE.Gadgeteer.Module;
using CTRE.Phoenix.Controller;
using CTRE.Phoenix;

namespace CANTester
{
    public class Program
    {
        /* Create a gamepad */
        CTRE.Phoenix.Controller.GameController myGamepad = new CTRE.Phoenix.Controller.GameController(new CTRE.Phoenix.UsbHostDevice(0));
        float povChangeValue = 0.01F;
        float deadBand = 0.1F;
        uint safetyButton = 16;
        float motorSpeed;
        string motorSpeedString;
        CTRE.Phoenix.Controller.GameControllerValues gv = new CTRE.Phoenix.Controller.GameControllerValues();

        /* The display board */
        DisplayModule displayModule = new DisplayModule(CTRE.HERO.IO.Port1, DisplayModule.OrientationType.Landscape);

        /* lets pick a font */
        Font smallFont = Properties.Resources.GetFont(Properties.Resources.FontResources.small);
        Font bigFont = Properties.Resources.GetFont(Properties.Resources.FontResources.NinaB);

        VerticalGauge motorSpeedGuage;
        DisplayModule.LabelSprite labelFx;
        DisplayModule.LabelSprite labelSafetyBtnState;
        DisplayModule.LabelSprite labelSafetyMsg;
        DisplayModule.LabelSprite labelSpeed;
        DisplayModule.LabelSprite labelTalon;
        DisplayModule.LabelSprite labelTitle;
        
        public void UpdateGauge(VerticalGauge gauge, float axis)
        {
            axis += 1.0f; // [0,2]
            axis *= 0.5f; // [0,1]
            gauge.Value = (int)(axis * gauge.MaxValue);
        }

        public void RunForever()
        {
            Boolean lastHAT = false;
            Boolean lastPOV0 = false;
            Boolean lastPOV4 = false;
            float axis1;

            motorSpeedGuage = new VerticalGauge(displayModule, 5, 5, 30, 10, DisplayModule.Color.Cyan, DisplayModule.Color.Blue);
            labelTitle = displayModule.AddLabelSprite(bigFont, DisplayModule.Color.White, 40, 0, 80, 16);
            labelSafetyMsg = displayModule.AddLabelSprite(smallFont, DisplayModule.Color.White, 40, 20, 100, 16);
            labelSafetyBtnState = displayModule.AddLabelSprite(bigFont, DisplayModule.Color.White, 0, 100, 159, 16);
            labelSpeed = displayModule.AddLabelSprite(smallFont, DisplayModule.Color.White, 0, 50, 100, 15);
            labelFx = displayModule.AddLabelSprite(smallFont, DisplayModule.Color.White, 0, 85, 100, 15);
            labelTalon = displayModule.AddLabelSprite(smallFont, DisplayModule.Color.White, 0, 70, 100, 15);

            /* add the motor controllers */
            CTRE.Phoenix.MotorControl.CAN.TalonSRX talonFwd;
            bool useTalonFwd = true;
            CTRE.Phoenix.MotorControl.CAN.TalonSRX talonRev;
            bool useTalonRev = true;

            CTRE.Phoenix.MotorControl.CAN.TalonFX fxFwd;
            bool useFxFwd = true;
            CTRE.Phoenix.MotorControl.CAN.TalonFX fxRev;
            bool useFxRev = true;
            
            talonFwd = new CTRE.Phoenix.MotorControl.CAN.TalonSRX(0);
            talonFwd.IsFwdLimitSwitchClosed();
            if (talonFwd.GetLastError() != 0)
            {
                useTalonFwd = false;
                Debug.Print("useTalonFwd is " + useTalonFwd);
            }

            talonRev = new CTRE.Phoenix.MotorControl.CAN.TalonSRX(1);
            talonRev.IsFwdLimitSwitchClosed();
            if (talonRev.GetLastError() != 0)
            {
                useTalonRev = false;
                Debug.Print("useTalonRev is " + useTalonRev);
            }

            fxFwd = new CTRE.Phoenix.MotorControl.CAN.TalonFX(2);
            fxFwd.IsFwdLimitSwitchClosed();
            if (fxFwd.GetLastError() != 0)
            {
                useFxFwd = false;
                Debug.Print("useFxFwd is " + useFxFwd);
            }

            fxRev = new CTRE.Phoenix.MotorControl.CAN.TalonFX(3);
            fxRev.IsFwdLimitSwitchClosed();
            if (fxRev.GetLastError() != 0)
            {
                useFxRev = false;
                Debug.Print("useFxRev is " + useFxRev);
            }

            /* loop forever */
            while (true)
            {
                labelTitle.SetText("CANTester");
                labelTitle.SetColor(DisplayModule.Color.Green);
                labelSafetyMsg.SetText("Safety is LB Button");
                labelSafetyMsg.SetColor(DisplayModule.Color.Green);
                labelTalon.SetText("Talon 0/1: " + useTalonFwd + "/" + useTalonRev);
                labelTalon.SetColor(DisplayModule.Color.Green);
                labelFx.SetText("Fx 0/1: " + useFxFwd + "/" + useFxRev);
                labelFx.SetColor(DisplayModule.Color.Green);
                /* Make sure the gamecontroller is still connected */
                if (myGamepad.GetConnectionStatus() == CTRE.Phoenix.UsbDeviceConnection.Connected)
                {
                    myGamepad.GetAllValues(ref gv);
                    axis1 = myGamepad.GetAxis(1);

                    /*
                     * Make sure the safetyButton is being pressed, if not shut things down)
                     */
                    if ((gv.btns & safetyButton) == safetyButton)
                    {
                        labelSafetyBtnState.SetText("Safety is pressed");
                        labelSafetyBtnState.SetColor(DisplayModule.Color.Green);
                        /*
                         * If HAT up (pov == 0) is pressed then increase the speed
                         * If HAT down (pov == 4) is pressed then decrease the speed
                         * Otherwise use left stick y access for speed
                         */
                        Debug.Print("buttons are " + ((gv.btns & safetyButton) == safetyButton));
                        if (gv.pov == 0 && !lastPOV0)
                        {
                            motorSpeed -= povChangeValue;
                            motorSpeed = (float)System.Math.Max(motorSpeed, -1.0F);
                            lastHAT = true;
                            lastPOV0 = true;
                            lastPOV4 = false;
                        }
                        else if (gv.pov == 4 && !lastPOV4)
                        {
                            motorSpeed += povChangeValue;
                            motorSpeed = (float)System.Math.Min(motorSpeed, 1.0F);
                            lastHAT = true;
                            lastPOV0 = false;
                            lastPOV4 = true;
                        }
                        else
                        {
                            if (System.Math.Abs(axis1) <= deadBand)
                            {
                                if (!lastHAT)
                                {
                                    motorSpeed = 0.0F;
                                    lastPOV0 = false;
                                    lastPOV4 = false;
                                }
                            }
                            else
                            {
                                lastHAT = false;
                                lastPOV0 = false;
                                lastPOV4 = false;
                                motorSpeed = axis1;
                            }
                            /*
                             * Prevent stuck or held down buttons
                             */
                            if (gv.pov != 0)
                            {
                                lastPOV0 = false;
                            }
                            if (gv.pov != 4)
                            {
                                lastPOV4 = false;
                            }
                        }
                        UpdateGauge(motorSpeedGuage, motorSpeed);
                        motorSpeedString = motorSpeed.ToString("f");
                        Debug.Print(motorSpeedString);
                        labelSpeed.SetText("Speed: " + motorSpeedString);
                        if (motorSpeed > deadBand)
                        {
                            labelSpeed.SetColor(DisplayModule.Color.Red);
                        }
                        else if (motorSpeed < -deadBand)
                        {
                            labelSpeed.SetColor(DisplayModule.Color.Green);
                        }
                        else
                        {
                            labelSpeed.SetColor(DisplayModule.Color.Yellow);
                        }
                    } else
                    {
                        labelSafetyBtnState.SetText("Safety is not pressed!");
                        labelSafetyBtnState.SetColor(DisplayModule.Color.Red);
                        motorSpeed = 0.0F;
                        lastHAT = false;
                        lastPOV0 = false;
                        lastPOV4 = false;
                    }
                } else
                {
                    labelSafetyBtnState.SetText("Joystick not connected");
                    labelSafetyBtnState.SetColor(DisplayModule.Color.Blue);
                    motorSpeed = 0.0F;
                    lastHAT = false;
                    lastPOV0 = false;
                    lastPOV4 = false;
                }
                if (useTalonFwd)
                {
                    talonFwd.Set(CTRE.Phoenix.MotorControl.ControlMode.PercentOutput, motorSpeed);
                }
                if (useTalonRev)
                {
                    talonRev.Set(CTRE.Phoenix.MotorControl.ControlMode.PercentOutput, -motorSpeed);
                }
                if (useFxFwd)
                {
                    fxFwd.Set(CTRE.Phoenix.MotorControl.ControlMode.PercentOutput, motorSpeed);
                }
                if (useFxRev)
                {
                    fxRev.Set(CTRE.Phoenix.MotorControl.ControlMode.PercentOutput, -motorSpeed);
                }
                CTRE.Phoenix.Watchdog.Feed();

                /* wait a bit */
                System.Threading.Thread.Sleep(20);
            }
        }

        public static void Main()
        {
            new Program().RunForever();
        }
    }
}
