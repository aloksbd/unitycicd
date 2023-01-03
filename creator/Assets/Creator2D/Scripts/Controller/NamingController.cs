using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace ObjectModel
{
    public class NamingController
    {
        public static string GetName(string baseName)
        {
            // TODO: auto naming strategy
            return baseName;
        }


        public static string GetName(string baseName, List<string> siblingsName)
        {
            var availableNumber = 1;
            foreach (var siblingName in siblingsName)
            {

                if (siblingName.Contains(baseName))
                {
                    try
                    {
                        if (availableNumber < GetItemNameNumber(siblingName)) break;
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

        public static string GetName(string baseName, IEnumerable<VisualElement> siblings)
        {
            List<int> takenNumbers = new List<int>();
            foreach (var element in siblings)
            {
                Foldout item;
                if (element is Foldout)
                {
                    item = (Foldout)element;
                }
                else
                {
                    continue;
                }

                if (item.name.Contains(baseName))
                {
                    var num = GetItemNameNumber(item.name);
                    takenNumbers.Add(num);
                }
            }
            var expectedRange = Enumerable.Range(1, takenNumbers.Count + 1);
            var missingNumbers = expectedRange.Except(takenNumbers);
            var name = baseName + GetFormattedNumber(missingNumbers.ToList()[0]);
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
}