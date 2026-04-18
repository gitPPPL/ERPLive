const granulesContainer = document.querySelector(".granules");
const outerPath = document.querySelector("#p-path-outer");
const innerPath = document.querySelector("#p-path-inner");
const logo = document.querySelector(".logo");
const loader = document.getElementById("loader");
const main = document.getElementById("main");

const TOTAL_GRANULES = 180;
const HALF = TOTAL_GRANULES / 2;

// Create granules
const granules = [];
for (let i = 0; i < TOTAL_GRANULES; i++) {
    const g = document.createElement("div");
    g.className = "granule";
    granulesContainer.appendChild(g);
    granules.push(g);
}

// Sample BOTH paths (double-line P)
const outerLen = outerPath.getTotalLength();
const innerLen = innerPath.getTotalLength();

const points = granules.map((_, i) => {
    if (i < HALF) {
        const p = outerPath.getPointAtLength((i / HALF) * outerLen);
        return { x: p.x, y: p.y };
    } else {
        const p = innerPath.getPointAtLength(((i - HALF) / HALF) * innerLen);
        return { x: p.x, y: p.y };
    }
});

// GSAP TIMELINE
const tl = gsap.timeline({ defaults: { ease: "power4.inOut" } });

// Scatter in 3D
tl.fromTo(granules, {
    x: () => gsap.utils.random(-160, 160),
    y: () => gsap.utils.random(-160, 160),
    z: () => gsap.utils.random(-300, 300),
    opacity: 0
}, {
    opacity: 1,
    duration: 0.1
});

// Cinematic rotation
tl.to(".granules", {
    duration: 1.2,
    rotateX: 360,
    rotateY: 360
});

// Form exact double-line "P"
tl.to(granules, {
    duration: 2,
    x: i => points[i].x,
    y: i => points[i].y,
    z: 0,
    stagger: 0.01
});

// Hold
tl.to({}, { duration: 0.4 });

// Dissolve granules
tl.to(granules, {
    duration: 0.6,
    scale: 0,
    opacity: 0,
    stagger: 0.005
});

// Reveal logo
tl.to(logo, {
    duration: 0.8,
    opacity: 1,
    scale: 1
}, "-=0.6");

// Exit loader
tl.to(loader, {
    duration: 0.6,
    opacity: 0,
    onComplete: () => {
        loader.style.display = "none";   // Remove loader
        document.body.style.overflow = "auto"; // Enable scrolling
    }
});

// Show main content (StartPage content)
tl.to(main, {
    duration: 0.6,
    opacity: 1
}, "-=0.5");

// Redirect to Home Page after the loader finishes
tl.add(() => {
    setTimeout(() => {
        window.location.assign('/Home/Index'); // Redirect to the home page
    }, 150); // Delay of 500ms after loader finishes
});




