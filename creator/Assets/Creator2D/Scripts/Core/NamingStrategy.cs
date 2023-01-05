using System.Collections.Generic;
using System;
using UnityEngine;

namespace ObjectModel
{
    public class NamingStrategy
    {
        public static string GetName(string baseName)
        {
            // TODO: auto naming strategy
            return baseName;
        }

        public static string GetName(string baseName, List<IItem> siblings)
        {
            var availableNumber = 1;
            foreach (var item in siblings)
            {
                if (item.Name.Contains(baseName))
                {
                    try
                    {
                        if (availableNumber < GetItemNameNumber(item.Name)) break;
                    }
                    catch
                    {
                        Trace.Error("Item Name doesnot have numbered suffix");
                    }
                    availableNumber++;
                }
            }
            var name = baseName + GetFormattedNumber(availableNumber);
            return name;
        }

        public static string GetFormattedNumber(int number)
        {
            var hundredthNumber = number / 100;
            var tenthNumber = (number % 100) / 10;
            var onesNumber = (number % 10);
            return "" + hundredthNumber + tenthNumber + onesNumber;
        }

        public static int GetItemNameNumber(string itemName)
        {
            string currentItemNumberString = itemName.Substring(itemName.Length - 3);
            int currentItemNumber;

            bool isParsable = int.TryParse(currentItemNumberString, out currentItemNumber);

            if (isParsable)
            {
                return currentItemNumber;
            }
            throw new FormatException();
        }
    }

    public class FloorPlanStrategy
    {
        public static void AdjustUpperFloors(List<IItem> siblings, int floorNumber, int adjustByNumber, float adjustByHeight)
        {
            foreach (var item in siblings)
            {
                if (item.Name.Contains(WHConstants.FLOOR_PLAN))
                {
                    try
                    {
                        int itemFloorNumber = NamingStrategy.GetItemNameNumber(item.Name);
                        if (itemFloorNumber >= floorNumber)
                        {
                            item.SetName(WHConstants.FLOOR_PLAN + NamingStrategy.GetFormattedNumber(itemFloorNumber + adjustByNumber));
                            var weakPosition = item.GetComponent<IHasPosition>();
                            if (weakPosition.IsAlive)
                            {
                                ((IHasPosition)weakPosition.Target).MoveBy(new Vector3(0, adjustByHeight, 0));
                            }
                        }
                    }
                    catch
                    {
                        Trace.Error("Item Name doesnot have numbered suffix");
                    }
                }
            }
        }
    }
}