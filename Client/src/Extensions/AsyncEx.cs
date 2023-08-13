using System.Threading.Tasks;

namespace Carbon.Extensions;

public class AsyncEx
{
	public static async Task WaitForSeconds(float seconds)
	{
		await Task.Delay((int)(seconds * 1000f));
	}
}
