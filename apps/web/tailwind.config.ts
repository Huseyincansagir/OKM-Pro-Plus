import type { Config } from "tailwindcss";

const config: Config = {
  content: ["./src/**/*.{js,ts,jsx,tsx,mdx}"],
  theme: {
    extend: {
      colors: {
        navy: {
          950: "#0B1F33",
          900: "#102A43",
          800: "#173B5E",
        },
        teal: {
          500: "#0F9D9A",
          600: "#087F7C",
          700: "#05605E",
        },
        surface: {
          50: "#F8FAFC",
          100: "#F1F5F9",
          200: "#E2E8F0",
        },
        amber: {
          500: "#D97706",
          100: "#FEF3C7",
        },
        danger: {
          500: "#DC2626",
          100: "#FEE2E2",
        },
        success: {
          500: "#16A34A",
          100: "#DCFCE7",
        },
      },
      boxShadow: {
        subtle: "0 1px 3px rgb(15 23 42 / 0.08)",
      },
    },
  },
  plugins: [],
};

export default config;
