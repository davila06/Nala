import { useState, useEffect } from "react";

export function useProgressiveImage(
  highRes: string | null | undefined,
  placeholder = "",
) {
  const [src, setSrc] = useState(placeholder || highRes || "");
  const [isLoaded, setIsLoaded] = useState(!highRes);

  useEffect(() => {
    if (!highRes) {
      setSrc(placeholder);
      setIsLoaded(true);
      return;
    }

    setSrc(placeholder || highRes);
    setIsLoaded(false);

    const img = new Image();
    img.src = highRes;
    img.onload = () => {
      setSrc(highRes);
      setIsLoaded(true);
    };
    img.onerror = () => {
      setSrc(placeholder);
      setIsLoaded(true);
    };

    return () => {
      img.onload = null;
      img.onerror = null;
    };
  }, [highRes, placeholder]);

  return { src, isLoaded };
}
