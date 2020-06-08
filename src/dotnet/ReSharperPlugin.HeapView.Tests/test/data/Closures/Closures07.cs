using System;
using System.Collections;

class LocalFunctions {
  void Delegate01(int p) {
    void Local() {
      Action a = Local;
      Delegate01();
      p++;
    }
  }

  void Delegate02(int p) {
    void Local() {
      p++;
    }

    Action a = Local;
  }
    
  void Delegate03(int p) {
    void Local() {
      p++;
    }

    Action b = () => {
      Action a = Local;
    };
  }

  void NotInvoked01(int p) {
    void Local() {
      p++;
    }
  }
  
  void NotInvoked02(int p) {
    void Local() {
      p++;
      Local();
    }
  }
    
  void Invocation01(int p) {
    void Local() {
      p++;
    }
    Local();
  }

  void Invocation02(int p) {
    void Local() {
      p++;
      Local();
    }
    Local();
  }

  void Invocation03(int p) {
    async void Local() {
      p++;
    }
    void Local2() { p++; }
    Local();
    Local2();
  }

  void Invocation04(int p) {
    IEnumerable Local() {
      p++;
      yield break;
    }
    void Local2() { p++; }
    Local();
    Local2();
  }
}