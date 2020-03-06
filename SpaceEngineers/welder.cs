using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        IMyCargoContainer WelderContainer, CargoContainers;        
        IMyAssembler AssemblerQueue; //Only queue assembler, no need for making stuff because i want to integrate this with ISY`s TIM etc.        
        IMyTextPanel WelderLCD, ErrorLCD;    
        

        public Program()
        {
            


            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            AssemblerQueue = GridTerminalSystem.GetBlockWithName("Assembler BuildList") as IMyAssembler;            
            WelderContainer = GridTerminalSystem.GetBlockWithName("Welder Container") as IMyCargoContainer; //This need to be change to get all invetory on welding ship, including connectors, welders and containers
            CargoContainers = GridTerminalSystem.GetBlockWithName("Small Cargo Container") as IMyCargoContainer; //This need to be change to exclude inventories on welding ship and take stuff from inventory at base    
            WelderLCD = GridTerminalSystem.GetBlockWithName("LCD Welder") as IMyTextPanel;
            ErrorLCD = GridTerminalSystem.GetBlockWithName("LCD Error") as IMyTextPanel;

            

        }


        public void Main(string argument, UpdateType updateSource)
        {
            ;
            switch (argument)
            {
                case "clear":
                    ClearAssemblerQueue(AssemblerQueue, WelderContainer, CargoContainers);
                    break;
                default:
                    
                    break;
            }

            PrintQueue(WelderContainer, WelderLCD, CargoContainers, ErrorLCD);

        }

        public void ClearAssemblerQueue(IMyAssembler AssemblerQueue, IMyCargoContainer WelderContainer, IMyCargoContainer CargoContainers) //This clear queue in assebler and remove items from welding ship container
            {
            AssemblerQueue.ClearQueue();
            for (int i = WelderContainer.GetInventory(0).ItemCount - 1; i >= 0; i--)
            {
                if (CargoContainers.GetInventory(0).CanItemsBeAdded(WelderContainer.GetInventory(0).GetItemAt(i).Value.Amount, WelderContainer.GetInventory(0).GetItemAt(i).Value.Type))
                {
                    WelderContainer.GetInventory(0).TransferItemTo(CargoContainers.GetInventory(0), WelderContainer.GetInventory(0).GetItemAt(i).Value);
                }
            }
        }

        

        public string CreateBlueprintName(string name)
        {
            switch (name)
            {
                case "RadioCommunicationComponent": name = "RadioCommunication"; break;
                case "ComputerComponent": name = "Computer"; break;
                case "ReactorComponent": name = "Reactor"; break;
                case "DetectorComponent": name = "Detector"; break;
                case "ConstructionComponent": name = "Construction"; break;
                case "ThrustComponent": name = "Thrust"; break;
                case "MotorComponent": name = "Motor"; break;
                case "ExplosivesComponent": name = "Explosives"; break;
                case "GirderComponent": name = "Girder"; break;
                case "GravityGeneratorComponent": name = "GravityGenerator"; break;
                case "MedicalComponent": name = "Medical"; break;
                case "NATO_25x184mmMagazine": name = "NATO_25x184mm"; break;
                case "NATO_5p56x45mmMagazine": name = "NATO_5p56x45mm"; break;

            }
            return name;

        }

        

        public void PrintQueue(IMyCargoContainer Container, IMyTextPanel LCD, IMyCargoContainer CargoContainers, IMyTextPanel ErrorLCD) //all code is in one function for now, need to be separate
        {
            IDictionary<string, int> componentDict = new Dictionary<string, int>();
            componentDict.Add("BulletproofGlass", 0);
            componentDict.Add("Computer", 0);
            componentDict.Add("Construction", 0);
            componentDict.Add("Detector", 0);
            componentDict.Add("Display", 0);
            componentDict.Add("Explosives", 0);
            componentDict.Add("Girder", 0);
            componentDict.Add("GravityGenerator", 0);
            componentDict.Add("InteriorPlate", 0);
            componentDict.Add("LargeTube", 0);
            componentDict.Add("Medical", 0);
            componentDict.Add("MetalGrid", 0);
            componentDict.Add("Motor", 0);
            componentDict.Add("PowerCell", 0);
            componentDict.Add("RadioCommunication", 0);
            componentDict.Add("Reactor", 0);
            componentDict.Add("SmallTube", 0);
            componentDict.Add("SolarCell", 0);
            componentDict.Add("SteelPlate", 0);
            componentDict.Add("Thrust", 0);
            string output = "";
            string error = "";

            
            //This part gets item list from assembler
            List<MyProductionItem> QueueList = new List<MyProductionItem>();             
            AssemblerQueue.GetQueue(QueueList);
            for (int k = 0; k < QueueList.Count; k++)
            {
                for (int i = 0; i < componentDict.Count; i++)
                {
                    string name = componentDict.Keys.ElementAt(i);
                    if (CreateBlueprintName(QueueList[k].BlueprintId.SubtypeName.ToString()) == name) { componentDict[name] += QueueList[k].Amount.ToIntSafe(); }
                }
            }
            for (int k = 0; k < componentDict.Count; k++)
            {
                if (componentDict[componentDict.Keys.ElementAt(k)] > 0)
                {
                    output += componentDict.Keys.ElementAt(k) + ": " + componentDict[componentDict.Keys.ElementAt(k)].ToString() + " / " + (float)Container.GetInventory(0).GetItemAmount(new MyItemType("MyObjectBuilder_Component", componentDict.Keys.ElementAt(k))) + "\n";
                }
            }            

            
            
            //Write to LCD
            LCD.ContentType = ContentType.TEXT_AND_IMAGE;
            LCD.WriteText(output);

            
            //Move items to welder ship
            for (int k = 0; k < componentDict.Count; k++)                
            {

                int req = componentDict[componentDict.Keys.ElementAt(k)];
                float itemsInWelder = (float)Container.GetInventory(0).GetItemAmount(new MyItemType("MyObjectBuilder_Component", componentDict.Keys.ElementAt(k)));
                error += "req: " + req + " itemsInWelder: " + itemsInWelder + "\n";

                if ( req > 0 && itemsInWelder < req)               
                {

                    MoveItem(CargoContainers, Container, componentDict.Keys.ElementAt(k), componentDict[componentDict.Keys.ElementAt(k)]);                    

                }
            }

            ErrorLCD.WriteText(error);


        }

        public static MyFixedPoint MoveItem(IMyCargoContainer from, IMyCargoContainer to, string type, MyFixedPoint? amount)
        {
            MyFixedPoint amountmoved = new MyFixedPoint();
            MyInventoryItem? item;
            bool moved;
            for (int i = from.GetInventory(0).ItemCount; i >= 0; i--)
            {
                item = from.GetInventory(0).GetItemAt(i);
                if (item.HasValue && item.Value.Type.SubtypeId == type)
                {
                    MyFixedPoint amounttomove = amount.HasValue
                        ? MyFixedPoint.Min(amount.Value - amountmoved, item.Value.Amount)
                        : item.Value.Amount;
                    moved = to.GetInventory(0).TransferItemFrom(from.GetInventory(0), item.Value, amounttomove);
                    if (!moved) return amountmoved;
                    amountmoved += amounttomove;
                    if (amountmoved >= amount) return amountmoved;
                }
            }
            return amountmoved;
        }
    }
}