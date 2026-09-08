import type { ImgHTMLAttributes } from "react";
import { useProgressiveImage } from "@/shared/hooks/useProgressiveImage";

interface ProgressiveImgProps
  extends Omit<ImgHTMLAttributes<HTMLImageElement>, "src"> {
  src: string | null | undefined;
  placeholder?: string;
  alt: string;
}

export function ProgressiveImg({
  src,
  placeholder,
  alt,
  className = "",
  style,
  ...rest
}: ProgressiveImgProps) {
  const { src: currentSrc, isLoaded } = useProgressiveImage(src, placeholder);

  return (
    <img
      src={currentSrc}
      alt={alt}
      className={className}
      style={{
        filter: isLoaded ? "blur(0)" : "blur(8px)",
        transition: "filter 0.45s ease",
        ...style,
      }}
      {...rest}
    />
  );
}
