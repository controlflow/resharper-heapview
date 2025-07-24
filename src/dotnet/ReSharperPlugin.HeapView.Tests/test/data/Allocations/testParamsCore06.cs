WithParams withParams = _ => { };
withParams(1, 2, 3);
withParams.Invoke(1, 2, 3);

public delegate void WithParams(params int[] xs);