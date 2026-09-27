namespace LabWorksApp.Services;

public class ArrayGeneratorService
{
    public const int MaxArraySize = 50;

    public int[] Generate(int minVal, int maxVal, int count)
    {
        if (count < 1 || count > MaxArraySize)
        {
            throw new ArgumentOutOfRangeException(nameof(count), $"Количество элементов должно быть от 1 до {MaxArraySize}.");
        }

        if (minVal > maxVal)
        {
            (minVal, maxVal) = (maxVal, minVal);
        }

        var result = new int[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = Random.Shared.Next(minVal, maxVal + 1);
        }

        return result;
    }
}
