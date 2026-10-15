(() => {
  const hero = document.querySelector(".hero");
  const canvas = document.querySelector("#hero-3d");
  const chips = Array.from(document.querySelectorAll(".feature-chip"));
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  let activeChip = 0;
  const setActiveChip = () => {
    chips.forEach((chip, index) => chip.classList.toggle("is-active", index === activeChip));
    activeChip = (activeChip + 1) % chips.length;
  };

  setActiveChip();
  if (!reducedMotion) {
    window.setInterval(setActiveChip, 2800);
  }

  const supportsWebGL = () => {
    try {
      const test = document.createElement("canvas");
      return Boolean(
        window.WebGLRenderingContext &&
          (test.getContext("webgl") || test.getContext("experimental-webgl"))
      );
    } catch (error) {
      return false;
    }
  };

  if (!window.THREE || !supportsWebGL()) {
    hero.classList.add("three-fallback");
    return;
  }

  const THREE = window.THREE;
  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(42, 1, 0.1, 100);
  const renderer = new THREE.WebGLRenderer({
    canvas,
    antialias: true,
    alpha: true,
    preserveDrawingBuffer: true,
    powerPreference: "high-performance",
  });

  renderer.setClearColor(0xffffff, 0);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputEncoding = THREE.sRGBEncoding;

  const pointer = new THREE.Vector2(0, 0);
  const targetPointer = new THREE.Vector2(0, 0);
  const clock = new THREE.Clock();
  const root = new THREE.Group();
  const hub = new THREE.Group();
  const orbit = new THREE.Group();
  const particlesGroup = new THREE.Group();

  scene.add(root);
  root.add(orbit);
  root.add(hub);
  root.add(particlesGroup);

  const palette = {
    blue: 0x3f98d4,
    green: 0x4db948,
    orange: 0xfa9418,
    coral: 0xfb694f,
    navy: 0x101b38,
    cyan: 0x79ddff,
    white: 0xffffff,
  };

  const ambient = new THREE.HemisphereLight(0xf7fbff, 0x61718b, 1.55);
  const key = new THREE.DirectionalLight(0xffffff, 2.2);
  const rim = new THREE.PointLight(0x79ddff, 2.1, 18);
  const warm = new THREE.PointLight(0xfa9418, 0.95, 16);

  key.position.set(-5, 6, 7);
  rim.position.set(5, 2.8, 5);
  warm.position.set(-2.5, -3, 4);
  scene.add(ambient, key, rim, warm);

  const makeMat = (color, options = {}) =>
    new THREE.MeshStandardMaterial({
      color,
      roughness: options.roughness ?? 0.34,
      metalness: options.metalness ?? 0.16,
      transparent: options.opacity !== undefined,
      opacity: options.opacity ?? 1,
      emissive: options.emissive ?? 0x000000,
      emissiveIntensity: options.emissiveIntensity ?? 0,
    });

  const glassMaterial = new THREE.MeshPhysicalMaterial({
    color: 0xffffff,
    roughness: 0.18,
    metalness: 0,
    clearcoat: 0.9,
    transparent: true,
    opacity: 0.76,
    side: THREE.DoubleSide,
  });

  const hubShell = new THREE.Mesh(new THREE.SphereBufferGeometry(1.08, 64, 64), glassMaterial);
  const core = new THREE.Mesh(
    new THREE.IcosahedronBufferGeometry(0.64, 2),
    makeMat(palette.blue, {
      roughness: 0.24,
      metalness: 0.34,
      emissive: palette.blue,
      emissiveIntensity: 0.22,
    })
  );
  hub.add(hubShell, core);

  const ringSpecs = [
    [1.42, palette.blue, 0.012, 0.95, 0],
    [1.72, palette.green, 0.01, 0.55, Math.PI / 2.6],
    [2.02, palette.orange, 0.012, 0.55, Math.PI / 4.2],
    [2.34, palette.coral, 0.009, 0.42, -Math.PI / 3],
  ];

  const rings = ringSpecs.map(([radius, color, tube, opacity, rotation]) => {
    const ring = new THREE.Mesh(
      new THREE.TorusBufferGeometry(radius, tube, 10, 128),
      new THREE.MeshBasicMaterial({
        color,
        transparent: true,
        opacity,
      })
    );
    ring.rotation.x = Math.PI / 2.15;
    ring.rotation.y = rotation;
    hub.add(ring);
    return ring;
  });

  const slabGeometry = new THREE.BoxBufferGeometry(1.18, 0.18, 0.22, 4, 1, 1);
  const slabSpecs = [
    [-0.62, 0.84, 0.1, -0.52, palette.blue, 1.18],
    [0.48, 0.36, 0.14, -0.52, palette.green, 0.94],
    [-0.72, -0.24, 0.18, -0.52, palette.orange, 0.9],
    [0.28, -0.84, 0.12, -0.52, palette.coral, 1.24],
  ];

  slabSpecs.forEach(([x, y, z, rotation, color, scale]) => {
    const slab = new THREE.Mesh(
      slabGeometry,
      makeMat(color, {
        roughness: 0.2,
        metalness: 0.28,
        emissive: color,
        emissiveIntensity: 0.06,
      })
    );
    slab.position.set(x, y, z);
    slab.rotation.z = rotation;
    slab.scale.x = scale;
    hub.add(slab);
  });

  const beam = new THREE.Mesh(
    new THREE.PlaneBufferGeometry(0.06, 2.95),
    new THREE.MeshBasicMaterial({
      color: palette.cyan,
      transparent: true,
      opacity: 0.32,
      side: THREE.DoubleSide,
      depthWrite: false,
    })
  );
  beam.rotation.z = Math.PI / 2;
  hub.add(beam);

  const createResumeStack = () => {
    const group = new THREE.Group();
    const pageMat = makeMat(0xffffff, { roughness: 0.42, metalness: 0.04 });
    const lineMats = [palette.blue, palette.green, palette.orange].map((color) =>
      makeMat(color, { emissive: color, emissiveIntensity: 0.08 })
    );

    for (let pageIndex = 0; pageIndex < 3; pageIndex += 1) {
      const page = new THREE.Mesh(new THREE.BoxBufferGeometry(1.18, 1.52, 0.045), pageMat);
      page.position.set(pageIndex * 0.08, pageIndex * 0.07, pageIndex * -0.055);
      page.rotation.z = -0.06 + pageIndex * 0.03;
      group.add(page);

      for (let lineIndex = 0; lineIndex < 4; lineIndex += 1) {
        const line = new THREE.Mesh(
          new THREE.BoxBufferGeometry(0.68 - lineIndex * 0.08, 0.035, 0.032),
          lineMats[(lineIndex + pageIndex) % lineMats.length]
        );
        line.position.set(-0.08, 0.38 - lineIndex * 0.22, 0.05 + pageIndex * -0.055);
        group.add(line);
      }
    }

    const check = new THREE.Mesh(
      new THREE.TorusBufferGeometry(0.23, 0.025, 10, 36),
      new THREE.MeshBasicMaterial({ color: palette.green })
    );
    check.position.set(0.42, -0.44, 0.18);
    group.add(check);
    return group;
  };

  const createKnowledgeStack = () => {
    const group = new THREE.Group();
    const colors = [palette.blue, palette.green, palette.orange, palette.coral];
    for (let layer = 0; layer < 4; layer += 1) {
      const block = new THREE.Mesh(
        new THREE.BoxBufferGeometry(0.82, 0.2, 0.82),
        makeMat(colors[layer], {
          roughness: 0.28,
          metalness: 0.22,
          emissive: colors[layer],
          emissiveIntensity: 0.08,
        })
      );
      block.position.y = layer * 0.25;
      block.rotation.y = layer * 0.42;
      group.add(block);
    }

    const base = new THREE.Mesh(
      new THREE.CylinderBufferGeometry(0.62, 0.72, 0.18, 48),
      makeMat(palette.navy, { roughness: 0.2, metalness: 0.4 })
    );
    base.position.y = -0.24;
    group.add(base);
    return group;
  };

  const createTalentNetwork = () => {
    const group = new THREE.Group();
    const nodeMat = makeMat(0xffffff, { roughness: 0.25, metalness: 0.16 });
    const accentMats = [palette.blue, palette.green, palette.coral].map((color) =>
      makeMat(color, { roughness: 0.24, metalness: 0.16, emissive: color, emissiveIntensity: 0.05 })
    );
    const positions = [
      [-0.64, 0.18, 0],
      [0.1, 0.56, 0.08],
      [0.62, -0.04, -0.06],
      [-0.02, -0.52, 0.04],
    ];

    positions.forEach((position, index) => {
      const node = new THREE.Mesh(new THREE.SphereBufferGeometry(0.18, 24, 24), nodeMat);
      node.position.set(...position);
      group.add(node);

      const badge = new THREE.Mesh(new THREE.SphereBufferGeometry(0.07, 16, 16), accentMats[index % 3]);
      badge.position.set(position[0] + 0.12, position[1] - 0.12, position[2] + 0.12);
      group.add(badge);
    });

    const lineMat = new THREE.LineBasicMaterial({
      color: palette.cyan,
      transparent: true,
      opacity: 0.72,
    });
    for (let i = 0; i < positions.length; i += 1) {
      const next = positions[(i + 1) % positions.length];
      const line = new THREE.Line(
        new THREE.BufferGeometry().setFromPoints([
          new THREE.Vector3(...positions[i]),
          new THREE.Vector3(...next),
        ]),
        lineMat
      );
      group.add(line);
    }
    return group;
  };

  const createInterviewDial = () => {
    const group = new THREE.Group();
    const face = new THREE.Mesh(
      new THREE.CylinderBufferGeometry(0.62, 0.62, 0.08, 64),
      makeMat(0xffffff, { roughness: 0.32, metalness: 0.05 })
    );
    face.rotation.x = Math.PI / 2;
    group.add(face);

    const edge = new THREE.Mesh(
      new THREE.TorusBufferGeometry(0.65, 0.035, 10, 64),
      new THREE.MeshBasicMaterial({ color: palette.coral })
    );
    edge.rotation.x = Math.PI / 2;
    group.add(edge);

    const handA = new THREE.Mesh(new THREE.BoxBufferGeometry(0.04, 0.42, 0.035), makeMat(palette.navy));
    const handB = new THREE.Mesh(new THREE.BoxBufferGeometry(0.04, 0.3, 0.035), makeMat(palette.orange));
    handA.position.y = 0.19;
    handB.position.x = 0.14;
    handB.position.y = 0.06;
    handB.rotation.z = -Math.PI / 2.8;
    group.add(handA, handB);
    return group;
  };

  const satellites = [
    [createResumeStack(), -2.7, 0.74, 0.62, 0.12],
    [createKnowledgeStack(), 2.68, 0.44, -0.26, 1.82],
    [createTalentNetwork(), -2.2, -1.52, -0.2, 3.32],
    [createInterviewDial(), 2.4, -1.42, 0.56, 4.8],
  ];

  satellites.forEach(([item, x, y, z, phase]) => {
    item.position.set(x, y, z);
    item.userData.phase = phase;
    orbit.add(item);
  });

  const connectorMat = new THREE.LineBasicMaterial({
    color: 0x7a869e,
    transparent: true,
    opacity: 0.34,
  });
  satellites.forEach(([, x, y, z]) => {
    const line = new THREE.Line(
      new THREE.BufferGeometry().setFromPoints([
        new THREE.Vector3(0, 0, 0),
        new THREE.Vector3(x * 0.75, y * 0.75, z),
      ]),
      connectorMat
    );
    orbit.add(line);
  });

  const particleCount = 240;
  const particleGeometry = new THREE.BufferGeometry();
  const particlePositions = new Float32Array(particleCount * 3);
  for (let i = 0; i < particleCount; i += 1) {
    const radius = 2.2 + Math.random() * 3.8;
    const angle = Math.random() * Math.PI * 2;
    particlePositions[i * 3] = Math.cos(angle) * radius;
    particlePositions[i * 3 + 1] = -2.2 + Math.random() * 4.4;
    particlePositions[i * 3 + 2] = Math.sin(angle) * radius * 0.36;
  }
  particleGeometry.setAttribute("position", new THREE.BufferAttribute(particlePositions, 3));
  const particles = new THREE.Points(
    particleGeometry,
    new THREE.PointsMaterial({
      color: palette.blue,
      size: 0.028,
      transparent: true,
      opacity: 0.48,
      depthWrite: false,
    })
  );
  particlesGroup.add(particles);

  const positionScene = () => {
    const width = canvas.clientWidth || window.innerWidth;
    const height = canvas.clientHeight || hero.clientHeight;
    renderer.setSize(width, height, false);
    camera.aspect = width / height;
    camera.updateProjectionMatrix();

    if (width < 720) {
      camera.position.set(0, 1.15, 8.7);
      root.position.set(1.22, -1.08, 0);
      root.scale.setScalar(0.66);
    } else if (width < 1120) {
      camera.position.set(0.18, 1.1, 8.35);
      root.position.set(1.1, -0.32, 0);
      root.scale.setScalar(0.9);
    } else {
      camera.position.set(0.8, 1.18, 7.5);
      root.position.set(2.74, -0.08, 0);
      root.scale.setScalar(0.92);
    }

    camera.lookAt(root.position.x, root.position.y, 0);
  };

  const onPointerMove = (event) => {
    targetPointer.x = (event.clientX / window.innerWidth - 0.5) * 2;
    targetPointer.y = (event.clientY / window.innerHeight - 0.5) * 2;
  };

  const animate = () => {
    const elapsed = clock.getElapsedTime();
    pointer.lerp(targetPointer, 0.045);

    if (!reducedMotion) {
      hub.rotation.y = elapsed * 0.28 + pointer.x * 0.16;
      hub.rotation.x = Math.sin(elapsed * 0.55) * 0.07 - pointer.y * 0.08;
      hub.position.y = Math.sin(elapsed * 0.9) * 0.08;
      orbit.rotation.y = elapsed * 0.1 + pointer.x * 0.08;
      orbit.rotation.x = Math.sin(elapsed * 0.35) * 0.04;
      particlesGroup.rotation.y = -elapsed * 0.035;
      beam.position.x = Math.sin(elapsed * 1.45) * 0.74;
      beam.material.opacity = 0.18 + Math.sin(elapsed * 2.3) * 0.08;
      rings.forEach((ring, index) => {
        ring.rotation.z = elapsed * (0.18 + index * 0.045) * (index % 2 ? -1 : 1);
        ring.rotation.y += 0.0015 * (index + 1);
      });
      satellites.forEach(([item, , , , phase], index) => {
        item.position.y += Math.sin(elapsed * 1.05 + phase) * 0.0018;
        item.rotation.y = Math.sin(elapsed * 0.55 + phase) * 0.26;
        item.rotation.x = Math.cos(elapsed * 0.42 + index) * 0.12;
      });
    }

    root.rotation.y += (pointer.x * 0.08 - root.rotation.y) * 0.025;
    root.rotation.x += (-pointer.y * 0.05 - root.rotation.x) * 0.025;
    renderer.render(scene, camera);
    document.documentElement.dataset.threeReady = "true";
    window.requestAnimationFrame(animate);
  };

  window.addEventListener("resize", positionScene);
  window.addEventListener("pointermove", onPointerMove, { passive: true });

  positionScene();
  animate();
})();
