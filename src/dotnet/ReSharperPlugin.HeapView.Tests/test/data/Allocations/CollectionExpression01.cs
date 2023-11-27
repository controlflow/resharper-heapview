using System.Collections.Generic;

int[] empty1 = [];
string[] empty2 = [];

int[] array1 = [111, 222];
string[] array2 = ["a", "b"];

int[] array3 = [..array1];
string[] array4 = [..args];

int[] array5 = [..array1, 1];
string[] array6 = [..args, "222"];

IEnumerable<string> ys = args;
string[] array7 = [..ys];
string[] array8 = [..ys, "aaa"];