using Core;
using UnityEngine;
using VContainer;

public class gameOpen : MonoBehaviour
{
    private HelloWorld _helloWorld;
    private IObjectResolver _resolver;

    [Inject]
    public void Construct(IObjectResolver resolver)
    {
        _resolver = resolver;
    }
    
    public void Update()
    {
        if (_helloWorld == null)
        {
            _helloWorld = _resolver.Resolve<HelloWorld>();
        }
        _helloWorld.Hello();
    }
}
