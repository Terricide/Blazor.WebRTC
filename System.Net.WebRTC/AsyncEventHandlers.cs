using System.Threading.Tasks;

namespace System.Net.WebRTC
{
    public delegate ValueTask AsyncEventHandler(object sender, EventArgs e);
    public delegate ValueTask AsyncEventHandler<T>(object sender, T e);
}
