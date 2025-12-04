import { useEffect } from "react";
import { useNavigate } from "react-router-dom";

export default function AuthCallback() {
  const navigate = useNavigate();

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const userId = params.get("userId");
    console.log("callback reached" + userId);
    if (userId) {
      localStorage.setItem("box_user_id", userId);
      console.log("userid exists" + userId);
      navigate("/upload");
    } else {
      navigate("/");
    }
  }, [navigate]);

  return <p>Completing login...</p>;
}
