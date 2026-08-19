import { SessionGate } from "@/components/auth/session-gate";
import { DesignSystemPreview } from "@/components/preview/design-system-preview";

export default function HomePage() {
  return (
    <SessionGate>
      <DesignSystemPreview />
    </SessionGate>
  );
}
