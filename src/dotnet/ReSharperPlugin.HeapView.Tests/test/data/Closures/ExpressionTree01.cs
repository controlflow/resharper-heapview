using System.Linq.Expressions;

Expression expr1 = () => 1;
Expression expr2 = () => args;
Expression expr3 = () => () => 1;
Expression expr4 = () => () => args;