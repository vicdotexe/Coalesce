import { createCoalesceVuetify } from "@/install";
import { mount, DOMWrapper } from "@vue/test-utils";
import { ArgumentsType } from "vitest";
import { defineComponent, h, nextTick } from "vue";

import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import $metadata from "./targets.metadata";

global.ResizeObserver ??= class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
global.cancelIdleCallback = function () {};

beforeEach(() => {
  document.body.childNodes.forEach((n) => n.remove());
});

const vuetify = createVuetify({ components, directives });
const coalesceVuetify = createCoalesceVuetify({
  metadata: $metadata,
});

const mountVuetify = function (
  component: ArgumentsType<typeof mount>[0],
  options: ArgumentsType<typeof mount>[1]
) {
  const wrapper = mount(component, {
    ...options,
    global: {
      plugins: [vuetify, coalesceVuetify],
    },
  });

  return wrapper;
} as typeof mount;

const mountApp = function (
  component: ArgumentsType<typeof mount>[0],
  options: ArgumentsType<typeof mount>[1]
) {
  const appWrapper = mount(
    defineComponent({
      render() {
        return h(components.VApp, () => [
          h(component as any, {
            ...options?.props,
            ...options?.attrs,
          }),
        ]);
      },
    }),
    {
      attachTo: document.body,
      global: {
        plugins: [vuetify, coalesceVuetify],
      },
    }
  );

  return appWrapper;
} as typeof mount;

export function getWrapper(selector = ".v-overlay-container") {
  return new DOMWrapper(document.querySelector(selector)!);
}

export async function delay(ms: number) {
  await new Promise((resolve) => setTimeout(resolve, ms));
}
export async function nextTicks(ticks: number) {
  for (let i = 0; i < ticks; i++) {
    await nextTick();
  }
}
export { nextTick } from "vue";

export { mountVuetify as mount, mountApp };