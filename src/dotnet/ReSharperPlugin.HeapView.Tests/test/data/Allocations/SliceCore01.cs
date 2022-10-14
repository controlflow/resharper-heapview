using System;
// ReSharper disable ConstantConditionalAccessQualifier
// ReSharper disable ConvertToConstant.Local

var slice01 = args[..]; // yes
var slice02 = args[1..]; // yes
var slice03 = args?[123..^11]; // yes

var str = args![0];
var slice04 = str[..]; // yes
var slice05 = str?[42..]; // yes

var start = 42;
var slice06 = str?[start..]; // yes

var range = 42..;
var slice07 = str?[range]; // yes

var span01 = args.AsSpan()[..];
var span02 = args[0].AsSpan()[..];