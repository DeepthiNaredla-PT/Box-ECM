import React from "react";
import { loginToBox } from "../Api";

export default function LoginPage() {
  return (
    <div style={{ textAlign: "center", paddingTop: 80 }}>
      <h2>Login with Box</h2>
      <button onClick={loginToBox}>Login</button>
    </div>
  );
}
