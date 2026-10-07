using System.Globalization;

namespace LabWorksApp.Services.Mathematics;

public class MathExpressionParser
{
    private enum TokenType
    {
        Number,
        Variable,
        Operator,
        Function,
        LeftParen,
        RightParen
    }

    private class Token
    {
        public TokenType Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public double NumberValue { get; set; }
        public int Precedence { get; set; }
        public bool RightAssociative { get; set; }
    }

    public static Func<double, double> Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Формула не может быть пустой.");

        var rawTokens = Tokenize(expression.Trim().ToLowerInvariant());
        var tokens = InsertImplicitMultiplication(rawTokens);
        var rpn = ConvertToRPN(tokens);

        // Проверяем валидность пробным вычислением
        try
        {
            EvaluateRPN(rpn, 1.0);
        }
        catch (DivideByZeroException)
        {
            // Деление на 0 в точке 1 возможно (например 1/(x-1)), это допустимо
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Ошибка в выражении '{expression}': {ex.Message}", ex);
        }

        return x => EvaluateRPN(rpn, x);
    }

    private static List<Token> Tokenize(string expr)
    {
        var tokens = new List<Token>();
        int i = 0;
        int n = expr.Length;

        while (i < n)
        {
            char c = expr[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                while (i < n && (char.IsDigit(expr[i]) || expr[i] == '.'))
                    i++;

                string numStr = expr[start..i];
                if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                    throw new ArgumentException($"Некорректное число: '{numStr}'");

                tokens.Add(new Token { Type = TokenType.Number, Text = numStr, NumberValue = val });
                continue;
            }

            if (c == '(')
            {
                tokens.Add(new Token { Type = TokenType.LeftParen, Text = "(" });
                i++;
                continue;
            }

            if (c == ')')
            {
                tokens.Add(new Token { Type = TokenType.RightParen, Text = ")" });
                i++;
                continue;
            }

            if (char.IsLetter(c))
            {
                int start = i;
                while (i < n && char.IsLetter(expr[i]))
                    i++;

                string word = expr[start..i];
                if (word == "x")
                {
                    tokens.Add(new Token { Type = TokenType.Variable, Text = "x" });
                }
                else if (word == "pi")
                {
                    tokens.Add(new Token { Type = TokenType.Number, Text = "pi", NumberValue = Math.PI });
                }
                else if (word == "e")
                {
                    tokens.Add(new Token { Type = TokenType.Number, Text = "e", NumberValue = Math.E });
                }
                else if (IsSupportedFunction(word))
                {
                    tokens.Add(new Token { Type = TokenType.Function, Text = word });
                }
                else
                {
                    throw new ArgumentException($"Неизвестный идентификатор или функция: '{word}'");
                }
                continue;
            }

            // Операторы: +, -, *, /, ^
            if (c is '+' or '-' or '*' or '/' or '^')
            {
                bool isUnary = (tokens.Count == 0 ||
                                tokens[^1].Type is TokenType.Operator or TokenType.LeftParen);

                if (isUnary && c == '-')
                {
                    tokens.Add(new Token
                    {
                        Type = TokenType.Operator,
                        Text = "u-",
                        Precedence = 3, // Ниже степени, но выше умножения
                        RightAssociative = true
                    });
                    i++;
                    continue;
                }

                if (isUnary && c == '+')
                {
                    // Унарный плюс пропускаем
                    i++;
                    continue;
                }

                int prec = c switch
                {
                    '+' or '-' => 1,
                    '*' or '/' => 2,
                    '^' => 4, // Степень имеет наивысший приоритет
                    _ => 0
                };

                tokens.Add(new Token
                {
                    Type = TokenType.Operator,
                    Text = c.ToString(),
                    Precedence = prec,
                    RightAssociative = (c == '^')
                });
                i++;
                continue;
            }

            throw new ArgumentException($"Недопустимый символ: '{c}'");
        }

        return tokens;
    }

    private static List<Token> InsertImplicitMultiplication(List<Token> rawTokens)
    {
        var result = new List<Token>();
        for (int i = 0; i < rawTokens.Count; i++)
        {
            var cur = rawTokens[i];
            if (i > 0)
            {
                var prev = rawTokens[i - 1];
                bool prevCanMultiply = prev.Type is TokenType.Number or TokenType.Variable or TokenType.RightParen;
                bool curCanBeMultiplied = cur.Type is TokenType.Variable or TokenType.Function or TokenType.LeftParen;

                if (prevCanMultiply && curCanBeMultiplied)
                {
                    result.Add(new Token
                    {
                        Type = TokenType.Operator,
                        Text = "*",
                        Precedence = 2,
                        RightAssociative = false
                    });
                }
            }
            result.Add(cur);
        }
        return result;
    }

    private static bool IsSupportedFunction(string name) => name switch
    {
        "sin" or "cos" or "tan" or "tg" or "sqrt" or "abs" or "exp" or "ln" or "log" or "log10" or "asin" or "acos" or "atan" => true,
        _ => false
    };

    private static List<Token> ConvertToRPN(List<Token> tokens)
    {
        var output = new List<Token>();
        var opStack = new Stack<Token>();

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Variable:
                    output.Add(token);
                    break;

                case TokenType.Function:
                    opStack.Push(token);
                    break;

                case TokenType.Operator:
                    while (opStack.Count > 0 && opStack.Peek().Type == TokenType.Operator)
                    {
                        var top = opStack.Peek();
                        if ((!token.RightAssociative && token.Precedence <= top.Precedence) ||
                            (token.RightAssociative && token.Precedence < top.Precedence))
                        {
                            output.Add(opStack.Pop());
                        }
                        else
                        {
                            break;
                        }
                    }
                    opStack.Push(token);
                    break;

                case TokenType.LeftParen:
                    opStack.Push(token);
                    break;

                case TokenType.RightParen:
                    bool matched = false;
                    while (opStack.Count > 0)
                    {
                        var top = opStack.Pop();
                        if (top.Type == TokenType.LeftParen)
                        {
                            matched = true;
                            break;
                        }
                        output.Add(top);
                    }

                    if (!matched)
                        throw new ArgumentException("Несогласованные скобки: отсутствует открывающая '('");

                    if (opStack.Count > 0 && opStack.Peek().Type == TokenType.Function)
                    {
                        output.Add(opStack.Pop());
                    }
                    break;
            }
        }

        while (opStack.Count > 0)
        {
            var top = opStack.Pop();
            if (top.Type is TokenType.LeftParen or TokenType.RightParen)
                throw new ArgumentException("Несогласованные скобки: отсутствует закрывающая ')'");
            output.Add(top);
        }

        return output;
    }

    private static double EvaluateRPN(List<Token> rpn, double x)
    {
        var stack = new Stack<double>();

        foreach (var token in rpn)
        {
            if (token.Type == TokenType.Number)
            {
                stack.Push(token.NumberValue);
            }
            else if (token.Type == TokenType.Variable)
            {
                stack.Push(x);
            }
            else if (token.Type == TokenType.Operator)
            {
                if (token.Text == "u-")
                {
                    if (stack.Count < 1) throw new InvalidOperationException("Ошибка операнда унарного минуса.");
                    stack.Push(-stack.Pop());
                }
                else
                {
                    if (stack.Count < 2) throw new InvalidOperationException($"Недостаточно операндов для '{token.Text}'.");
                    double b = stack.Pop();
                    double a = stack.Pop();

                    double res = token.Text switch
                    {
                        "+" => a + b,
                        "-" => a - b,
                        "*" => a * b,
                        "/" => Math.Abs(b) < 1e-15 ? throw new DivideByZeroException("Деление на ноль.") : a / b,
                        "^" => Math.Pow(a, b),
                        _ => throw new InvalidOperationException($"Неизвестный оператор: {token.Text}")
                    };

                    stack.Push(res);
                }
            }
            else if (token.Type == TokenType.Function)
            {
                if (stack.Count < 1) throw new InvalidOperationException($"Недостаточно операндов для функции '{token.Text}'.");
                double arg = stack.Pop();

                double res = token.Text switch
                {
                    "sin" => Math.Sin(arg),
                    "cos" => Math.Cos(arg),
                    "tan" or "tg" => Math.Tan(arg),
                    "sqrt" => arg < 0 ? throw new ArgumentException("Корень из отрицательного числа.") : Math.Sqrt(arg),
                    "abs" => Math.Abs(arg),
                    "exp" => Math.Exp(arg),
                    "ln" or "log" => arg <= 0 ? throw new ArgumentException("Логарифм неположительного числа.") : Math.Log(arg),
                    "log10" => arg <= 0 ? throw new ArgumentException("Логарифм неположительного числа.") : Math.Log10(arg),
                    "asin" => Math.Asin(arg),
                    "acos" => Math.Acos(arg),
                    "atan" => Math.Atan(arg),
                    _ => throw new InvalidOperationException($"Неизвестная функция '{token.Text}'.")
                };

                stack.Push(res);
            }
        }

        if (stack.Count != 1)
            throw new InvalidOperationException("Синтаксическая ошибка: неверная структура выражения.");

        return stack.Pop();
    }
}
