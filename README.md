# **Example-Based Sampling for Depth of Field in Unity** 

Matthew McConnell University of Calgary Calgary, AB, Canada 

#### **ACM Reference Format:** 

Matthew McConnell. 2026. Example-Based Sampling for Depth of Field in Unity. In _Proceedings of ACM Conference (Conference’17)._ ACM, New York, NY, USA, 8 pages. https://doi.org/10.1145/nnnnnnn.nnnnnnn 



**Figure 1: LDBN Sampler - 576 Samples** 

## **1 INTRODUCTION** 

Sampling techniques are an essential component of computational applications, forming the foundation of many areas including computer graphics [12]. In rendering, the choice of sampling strategy directly impacts various domains such as Monte Carlo integration, procedural generation, and Depth of Field (DoF) effects [5]. There exists a multitude of traditional methods for sampling, however, they often rely on analytical information or specialized construction rules [5]. 

Recent advances in machine learning (ML) have introduced the potential for example-based sampling, where models learn to create complex distributions directly from data-driven examples. Doignies et al. [5] introduced a diffusion-based example-sampler that can successfully learn a wide range of 2D sampling patterns, including blue noise, without explicit formulative definitions. 

This project aims to implement the example-based diffusion sampling method described above, and integrate it into a real-time rendering application through DoF in Unity. A learned sampler, powered by a Python backend will integrate into the Unity game 

Permission to make digital or hard copies of all or part of this work for personal or classroom use is granted without fee provided that copies are not made or distributed for profit or commercial advantage and that copies bear this notice and the full citation on the first page. Copyrights for components of this work owned by others than ACM must be honored. Abstracting with credit is permitted. To copy otherwise, or republish, to post on servers or to redistribute to lists, requires prior specific permission and/or a fee. Request permissions from permissions@acm.org. _Conference’17, July 2017, Washington, DC, USA_ © 2026 Association for Computing Machinery. ACM ISBN 978-x-xxxx-xxxx-x/YY/MM...$15.00 https://doi.org/10.1145/nnnnnnn.nnnnnnn 

engine, which will in turn use the generated sample sets to render custom DoF effects. 

## **2 BACKGROUND & MOTIVATION** 

A wide range of samplers have been proposed for multiple domains, including Monte Carlo integration, rendering, image stippling, positioning of objects - or generally, to cover some space [5]. Traditional samplers can be effective in certain domains, particularly when information regarding their construction is known in advance and can be parameterized for a given domain. However, many sampling methods fall short when only examples of the given patterns exist, leaving it difficult to infer the sampling rules. Doignies et al. [5] propose a ML-based diffusion model that can "learn" high-quality sampling patterns from examples. They demonstrate that it is indeed possible to reproduce the complex nature of existing samplers without requiring previous information regarding analytical descriptions. They do so by leveraging neighborhood information from a uniform grid to implement fast convolutions on grids, utilizing a ML approach to support example-based sample learning [5]. 

Noise sampling is critical to the rendering pipeline, being primarily used to generate sequences of "semi-random" numbers [8]. Such use-cases include can include soft shadows, blurry reflections, ambient occlusion, volumetric fog, or importantly, DoF [8]. The distribution properties of these noise patterns directly affect the visual quality of images. Poorly distributed (white noise) samples can often cause low-frequency noise or grainy appearances. On the other hand, blue noise, a method of placing random points that are approximately evenly spaced, produces smoother characteristics [8] and prevents aliasing artifacts by replacing them with noise [9]. Blue noise sampling has been widely studied with a variety of proposed methods [10], with many being a combination of two classical algorithms; dart throwing [3] and relaxation [11]. Blue noise is traditionally more desirable than white noise, as it produces more visually uniform results with reduced artifacts. A comparison is shown in Figure 2. 



**Figure 2: Unfiltered ambient occlusion with white noise (left) and blue noise (right). Taken from [8].** 

Depth of Field (DoF) refers to the range of object distances that appear acceptably sharp in an image [12]. For physically based 

Conference’17, July 2017, Washington, DC, USA 

Matthew McConnell 

rendering, DoF simulation often involves integrating radiance over a lens aperture by sampling rays according to some distribution. As such, the chosen sampling pattern can directly influence image quality. These sampling patterns directly influence the quality of images, where structured sampling, such as Blue noise sampling [13], can reduce variance and produce smoother effects. This relationship between sampling patterns and perceptual quality makes DoF an ideal real-world rendering application for studying sample strategies. 

There are many ways to calculate realistic depth of field (DoF) in rendering applications, but almost all connect back to the thin-lens of full lens-based camera models as seen in PBRT’s projective and realistic camera frameworks [12]. For projective cameras, DoF is introduced by substituting the ideal pinhole representation with a finite aperture. Rays are then sampled over this aperture which intersect the focal plane at slightly different positions - producing a characteristic blur for objects not lying on the focal plane. PBRT’s realistic camera class [12] extends this by simulating a full lens system, using measured lens elements, multiple refraction descriptions, and physically accurate apertures. For both models, the amount of blur is governed by the circle of confusion (CoC), which is the projected size of a point after passing through a finite aperture - increasing as a function of the distance between the scene point and the focal plane. Additionally, each pixel’s radiance is given by an integral over both time and the lens aperture - making depth of field a Monte Carlo integration problem. As a result, the chosen sampling distribution directly affects variance, bokeh shape, and the presence of noise or aliasing artifacts in the rendered image. This strong reliance on sampling strategy makes DoF an ideal candidate for evaluating and comparing different 2D sampling strategies, particularly those concerning blue-noise or low-discrepancy patterns. 

Although PBRT’s thin-lens (and realistic camera) models describe DoF as a full Monte-Carlo integration over lens area and time - real time engines (including Unity’s Universal Render Pipeline) do not trace multiple rays per pixel. Instead, they approximate DoF in **screen space** by manipulating the rendered image according to depth information. This rasterization-based approach derives the CoC from the hardware depth buffer. The corresponding blur is then reconstructed by gathering neighborhood pixel colors inside a radius proportional to the CoC. Because screen-space, rasterizationbased DoF approaches (as the one implemented in this project) leverages this neighborhood information via a sampling kernel - the choice of sampler is as equally important as we saw in the physically based approach. 

## **3 RELATED WORKS** 

## **3.1 Example-Based Sampling with Diffusion Models** 

Doignies et al. [5] introduce "Example-Based Sampling with Diffusin Models" - a diffusion-based approach for generating 2D point sets that replicate the properties of classical sampling algorithms. Notably, their approach is a learned approach. Rather than generating point sets via hard-set algorithms, rules, or other processes, the authors generate point sets from previous example distributions. Although diffusion models offer high expressive power for learning 



<!-- Start of picture text -->
ENNIS<br>ah<br>Lali|<br><!-- End of picture text -->

**Figure 3: Grid Representation from Doignies et al. [5]** 

fine-grained spatial structure of patterns, they are _inherently convolutional_ and are therefore impractical for dealing with scattered data such as point sets [5]. To overcome this, the authors map each point set to a uniform grid (as seen in Figure 3) through an **optimal transport (OT) assignment** , which encodes samples as offsets from their assigned grid cells [5]. This grid representation leverages neighborhood information, thereby allowing for the benefit of fast convolutions on grids when used in convolutional U-nets. Such an approach allows their model to learn sampling patterns without requiring explicit information of the underlying sampling patterns, such as power spectra or discrepancy metrics. 

Using this setup, the authors train diffusion models across various samplers, demonstrating that a single model can learn vastly different point sets - from stratified Sobol’ / Owen sequences to blue-noise patterns such as GBN, LDBN, and Poisson disk patterns. Importantly, their model generalizes to **unseen sample counts** , even though the networks are only trained on grids of sizes 8 × 8 _,_ 16 × 16 _,_ 32 × 32. Additionally, the generated samples from their model reproduces key properties of the underlying sampler, including power spectrum, distance statistics, optimal transport energy, discrepancy, and Monte-Carlo integration error [5]. Although not relevant for this project, the authors also demonstrate the model’s ability to handle non-uniform distributions - however, performance quickly degrades when local density of samples varies among examples of the training set [5]. Their approach also demonstrates that diffusion-based point generators are **fully differentiable** [5], allowing optimization within a given sampler’s distribution class. For instance, the authors show how to start from sliced optimal transport samples, and optimize the initial noise to enforce **additional low-discrepancy properties** , while keeping the results consistent point sets the model was trained on. 

For this project, the most essential aspects of Doignies et al’s. approach are a) the **OT grid representation** which makes diffusion feasible for point-set generation, b) the **example-based nature** of their model, allowing generated points to mimic many different (and popular samplers), and c) the **generalization across grid sizes** , which enables me to query the model with "relatively arbitrary" requested point counts, and obtain a remapping back to [0 _,_ 1]<sup>2</sup> . This approach and the properties within provide an excellent choice to satisfy the needs of my real-time DoF pipeline, allowing the connection of this method to a real-world rendering application. 

Conference’17, July 2017, Washington, DC, USA 

Example-Based Sampling for Depth of Field in Unity 

## **3.2 Other Relevant Materials** 

Although the previous foundational source covers the majority of the related work, there exists a few other materials that were useful for this project. 

Robert Cook’s paper on Stochastic Sampling in Computer Graphics [3] provides excellent motivation for the importance of sampling in computer graphics. Cook shows that stochastic sampling replaces the aliasing artifacts of regular point sampling with visually tolerable noise, and connects this approach to "distributed ray tracing", which enables realistic rendering effects such as motion blur, soft shadows, and depth of field. 

Li et al. [10] and Heck et al. [9] provide the necessary background information for the problem of blue noise sampling, by both connecting blue noise sampling to a variety of rendering applications as well as providing modern sampling approaches to achieve it. Li et al. [10] extend dart throwing and relaxation (two classical methods for isotropic blue noise sampling) to an anisotropic setting, while showing high-quality results and efficient computation. Heck et al. [9] synthesize two new types of blue noise patterns - step blue noise and single-peak blue noise, which utilize the mathematical relationship of the radial power spectrum to determine which power spectra can be used to construct blue noise point sets. Additionally, Sun et al. [13] present an analysis of blue noise powered line segment sampling, which preserves the blue-noise properties for use in a real-world rendering application of motion blur. 

Finally, a NVIDIA blog post by Joe Demers [4] and a Unity Universal Render Pipeline Tutorial by Jasper Flick [7] were instrumental in providing necessary context. 

## **4 METHODOLOGY & IMPLEMENTATION** 

## **4.1 Environment & Architecture** 

To decouple the computationally heavy sampling of the learned model with real-time rendering, the architecture has been split into two distinct parts. First, the Unity frontend, where all of the rendering implementation is found. This includes (but is not limited to), cameras, rendering of scenes, custom DoF shader, and user interaction (UI) handling. Second, a Python-backend, which exposes a FastAPI server on an **Ubuntu Server 24.04** . Although this choice is certainly not a structural design necessity (the exact same process could work via sub-processes on the same machine), my Linux server is the only device I own with an **Nvidia GPU** (GTX 1050ti), which the learned models (provided by Doignies et al.) requires due to various machine learning based dependencies (CUDA). As such, a simple Anaconda environment was set up on the server, to handle all of the sampling tasks, and **Unity 2021.3.45f2** was ran locally on my primary PC. 

## **4.2 Custom Depth of Field in Unity** 

I implement a four-pass screen-space post-processing shader that approximates a thin-lens camera in image space. This implementation is not a physically-based DoF model as we saw in [12]. Instead, it is a rasterized post-processing approximation, which takes advantage of Unity’s built in rendering pipeline and tools included in the Universal Render Pipeline (URP). Conceptually, it follows a similar structure as PBRT’s thin-lens model [12], parameterized by 

focal distance and lens aperture. However, instead of tracing rays through a physical lens, we reconstruct defocus blur from the **hardware depth buffer** . In each frame, I first compute a per-pixel circle of confusion (CoC) from the camera depth texture, apply a prefilter, and pack the CoC into the alpha channel of a color buffer. From here, a bokeh accumulation pass is performed utilizing a custom kernel created by the learned sampler. This accumulation pass uses the 2D sampling kernel to gather neighboring pixel colors based on the per-pixel CoC. Although this is performed in a rasterization manner, it is conceptually similar to a Monte Carlo estimator, where the blurred color at each pixel is computed as an average over samples drawn from the distribution defined on the aperture. The overall design is based on "Catlike Coding’s DoF blogpost" [7] - extended to support arbitrary sampling kernels provided from my learned sampler. 

_4.2.1 Circle of Confusion - Pass 0._ The first pass estimates the CoC radius per pixel from the depth buffer. Unity exposes the camera’s depth buffer as a Camera Depth Texture, which can then be sampled and converted to a linear eye-space distance using HLSL’s LinearEyeDepth() function. Let _𝑑_ be this new linear depth, and let _𝐹𝑑_ and _𝐹𝑟_ be focus distance and focus range respectively (user defined parameters) that control the focal plane and depth range over which blur ramps up. The normalized CoC value then becomes: 



which yields negative values for points **in front** of the focal plane, and positive values **behind it** . | _𝑐_ | = 1 represents the maximum blur radius. According to physically-based thin-lens models [12], the CoC diameter is derived from the Gaussian lens equation - being proportional to the lens diameter and distance to the focal plane. However, my mapping is a simplified, linearized version of this - where _𝐹𝑟_ controls how quickly the CoC grows with depth. The results of this pass are outputted as scalar CoC values, stored in a single-channel texture. Importantly, the sign of _𝑐_ is preserved, such that later passes can distinguish background blur from foreground blur (e.g, foreground objects should not be "smeared" by background CoC). 

_4.2.2 Prefilter - Pass 1._ The second pass combines scene color and CoC into a single RGBA buffer, while resolving various conflicts between foreground and background CoC at different depths. The trivial solution would be to simply read the CoC at each pixel, and store it into the alpha channel of the color buffer. However, this can cause edges to have bleeding artifacts, where large background CoC values can overwrite small foreground CoC values. To mitigate this, a 2x2 neighborhood around each pixel is sampled, and used to compute the minimum and maximum CoC values for that "window". The chosen CoC is dependent on overall magnitude, where the "largest" min or max value is selected. Let the four CoC values be _𝑐_ 0 _,𝑐_ 1 _,𝑐_ 2 _,𝑐_ 3 where each represents one of the values from the 2x2 neighborhood. 



This selection chooses the dominant blur layer (foreground or background) in the local neighborhood while preserving _𝑐_ ’s sign. The 

Conference’17, July 2017, Washington, DC, USA 

Matthew McConnell 

result is written as the alpha channel of a color buffer, with RGB storing the original scene color. 

_4.2.3 Bokeh - Pass 2._ The third pass performs the real meat of the DoF calculation - the actual bokeh integration. Here, I treat the CoC value stored in alpha as a _per-pixel blur radius_ , and approximate the blur by sampling the color buffer with a set offsets that represent a discrete aperture shape. In PBRT’s thin-lens camera, each image sample is generated by first sampling a point on the lens aperture, and then tracing a ray through the corresponding point on the plane of focus. My implementation mimics the conceptual idea of the resulting blur - but in screen space. Instead of casting multiple rays per pixel, I gather color contributions from neighboring pixels located within a radius proportional to the CoC. 

In a more formal sense, the shader receives an array of 2D vectors, representing the sampling kernel, and a count (kernel size). Notably, these are provided by the learned sampler. Each kernel value encodes a direction representing a point on the normalized aperture. For a given pixel with a CoC value _𝑐_ , and an aperture value _𝐴_ the blur radius is computed as: 



where saturate() clamps a given value to the range [0 _,_ 1]. Here we are essentially scaling by the current aperture value _𝐴_ . Then, for each kernel sample _𝑘_ (in our kernel _𝐾_ ) the texture coordinate is offset by: 



where _𝑑𝑘_ is the 2D direction stored in _𝐾_ [ _𝑘_ ] and _𝑇𝑥,𝑦_ represents Unity’s built in conversion from pixel units to texture coordinates. The bokeh shader then accumulates the color from the neighbourhood by sampling the input color buffer at these offset positions: 



and computes the final blurred color as the uniform average: 



_4.2.4 Postfilter / Reconstruction - Pass 3._ The final pass is essentially a "clean up" pass. Because the bokeh accumulation uses a finite number of kernel samples, the resultant image can still exhibit some high-frequency noise, especially when the kernel is sparse, or when the aperture radius is large. The final pass applies a simple 2x2 box filter over the bokeh image. Let _𝐼_ ( _𝑢, 𝑣_ ) denote the input image sampled at texture coordinates ( _𝑢, 𝑣_ ). Further, let _𝑇𝑥_ = _𝑊_ <u>1</u><sup>and</sup><sup>_𝑇𝑦_=</sup> _𝐻_<sup><u>1</u>be the horizontal and vertical texel sizes for</sup> an image with resolution _𝑊_ × _𝐻_ . The 2 × 2 box filter around each pixel is represented as: 



where the summation over the image at the four corners of a 2 × 2 neighborhood is averaged by the multiplication of<sup><u>1</u></sup> 4<sup>. This postfilter</sup> can essentially be interpreted as a low-cost reconstruction filter applied on top of the Monte-Carlo-esque sampling performed in the bokeh pass. In practice, it slightly blurs bokeh edges and hides residual sampling noise, providing more stable DoF. 

## **4.3 Unity Implementation as a URP Render Feature** 

The custom DoF effect is implemented as a URP render feature, that runs as a custom post-processing pass, being fully controlled through Unity’s volume framework. The bokeh sampling kernel (.npy) is provided from a learned model at runtime. A Volume Component (aptly named CustomDOF) is defined that exposes three parameters: 

### (1) Focal Distance 

### (2) Aperture 

- (3) Focal Length 

These are modeled as clamped float parameters, such that they can be used and manipulated at runtime through UI sliders. At runtime, this CustomDOF component is placed into a "Volume Manager" stack (essentially a post-processing stack, but for volumes), which "uses" the effect with its current configuration of parameters. 

In terms of rendering, the effect is registered as a scriptable render feature, which is Unity’s mechanism for injecting custom rendering logic into the URP. This component owns a small setting struct that lets me specify the DoF shader and the specific render pass event to inject into the pipeline. By default, the pass is scheduled _before rendering post processing_ , which means it runs _after_ opaque and transparent rendering, but before URP’s built-in post-processing tasks. During initialization, the render feature ensures that the custom DoF shader is available, constructs a material from it, and prepares a sampling kernel to be used in the bokeh pass. The sampling kernel is stored as a Vector4[] with a fixed maximum size (576). At runtime (as we will see below), the learned sampler fills up this Vector4[] with the correct point set based on the selected sampler. 

The actual rendering process is performed by an inner class derived from a scriptable render class in Unity’s URP setup. Temporary render targets are allocated - a single channel half-precision buffer for the CoC, and two RGBA half-precision buffers, used for prefitler and bokeh/postfilter stages respectively. All three buffers are allocated with zero depth bits, since only color data is needed. Depth information is derived from the URP’s shared Camera Depth Texture. When this process is executed, the CustomDOF volume component is resolved from the "volume stack". The three scalar parameters mentioned above are bound to their corresponding uniforms in the shader. Next, the pass selects the sampling kernel and passes it (and its count) to the relevant location in the shader. 

Once the parameters and kernel data is bound and initialized the four render passes, are orchestrated via Blit operations. The first blit takes the camera color target as input, and writes into the CoC render target - invoking the CoC pass (pass 0). The resulting texture is then published as a global texture so that it can be used / sampled further on. The second blit runs the prefilter pass (pass 1), which reads the original color and CoC texture, and writes a packed RGBA buffer - storing the CoC value in the alpha channel. The third blit executes the bokeh pass (pass 2), reading from the previously packed RGBA buffer and writing to a blurred result. In this pass, the sampling kernel offsets and aperture-scaled CoC radius determine the set of neighbor samples used for the blur at each pixel. Finally, the fourth blit runs the postfilter pass (pass 3), 

Conference’17, July 2017, Washington, DC, USA 

Example-Based Sampling for Depth of Field in Unity 

which applies a small reconstruction filter, and writes the final DoF image back into the camera’s color target. 

This setup leverages URP’s "extensibility" in a few ways. First, the DoF effect behaves like a post-processing effect that can be controlled through the volume manager. Second, it can be inserted at an appropriate point in the renderer’s graph of operations - allowing other effects to be stacked. Finally, a well-defined interface for sampling selection is exposed - for swapping in different bokeh sampling kernels. The underlying DoF effect does not consider how the kernel was produced, from the rendering perspective, it simply receives an array of 2D offsets and per-pixel blur radius that performs a screen-space approximation of a thin-lens camera. 

## **4.4 Diffusion-Based Sampling** 

_4.4.1 Diffusion-Based Sampling in Theory._ To obtain high-quality sampling patterns for my bokeh effect, I rely on the example-based diffusion samplers of Doignies et al. [5]. Their method learns a generative model that maps Gaussian noise to 2D point sets whose statistics match those of a reference sampler. These trained models can be sampled in _relatively_ real-time, with my **GTX 1050ti** taking ≈ 3 seconds per generation. 

For this project, a key difficulty is that the diffusion models are convolutional and naturally operate on grids, while sampling patterns are often represented as unordered point sets. The authors address this by introducing an optimal transport mapping between example point sets and a regular grid. As we will see, this leads us limited in the number of samples we can generate (in terms of size). During the data preparation step, each example point set is matched to a uniform grid using an OT solver, and the point coordinates are rearranged into an image-like tensor. This allows the diffusion U-Net to process point sets using standard 2D convolutions. At sampling, the model starts from Gaussian noise on this grids, runs the reverse diffusion process for a fixed number of timesteps, and outputs a grid of 2D coordinates. 

The diffusion network is _conditioned_ on a discrete distribution class or set of samplers - such that a single trained model can produce multiple families of point sets. These include: 

- **LDBN** : Low-Discrepancy Blue Noise that combines the spectral properties of blue noise with the stratification of lowdiscrepancy sequences (1D binary van der Corput sequences) [1]. 

- **GBN** : Gaussian Blue Noise, utilizing Gaussian kernels [2]. 

- **Poisson** : Poisson disk sampler, classical dart throwing approach [5]. 

- **Owen** : Sobol’ samples with Owen’s scrambling [5]. 

A comparison of raw samples can be seen in Figure 4. 

In this sense, it behaves essentially as a "meta-sampler": given a class label and a desired grid size, it creates a **fresh** point set with the spectral and discrepancy properties of the corresponding sampler. This is directly relevant to rendering, where many approaches often emphasize the fact that blue-noise and low-discrepancy sequences can **dramatically reduce** variance and aliasing [3], [8]. Overall, this diffusion sampler gives me access to similar distributions, but in a unified manner - where the model has **learned** from previous examples. 



**Figure 4: Raw samples: GBN (top left), Hammersley (top middle), LDBN (top right), Owen (bottom left), Poisson (bottom middle), White Noise (bottom right) n = 256** 

For this project, **I do not train the diffusion model myself** . Instead, the authors provide pre-trained checkpoints and configurations in their github [6]. For each of the samplers utilized in this project (and listed above), the diffusion model produces a 2D point set in [0 _,_ 1]<sup>2</sup> whose properties closely match the corresponding analytical sampler, but with **fast** and **uniform** generation. 

_4.4.2 Learned Sampling as a DoF Kernel Generator._ To ensure computation is minimized during the actual rendering process, and to keep Unity relatively lightweight, I wrapped the author’s sampler in a small Python "back-end" server. The core of this Python service is a FastAPI server that exposes a single endpoint: 

### POST /sample {sampler: "NAME", points: N} 

where "NAME" is the requested sampler, and _𝑁_ is the number of points requested. On the server side, I use a simple registry to map the requested sampler name to the corresponding diffusion model checkpoint and configuration, which have been **pre-downloaded** via a simple helper bash script. 

Given a target sample count _𝑁_ (as provided by Unity), the server first chooses a square grid resolution, using a simple Python function. This maps _𝑁_ to supported grid sizes (if able), corresponding to native sizes of: 

### _𝐻_ × _𝑊_ ∈{8 × 8 _,_ 16 × 16 _,_ 24 × 24 _,_ 32 × 32} 

corresponding to native sampler sizes of 64 _,_ 256 _,_ 576 _,_ 1024 points respectively. The diffusion sampler (again, provided by Doignies et al. [5]) is then launched as a subprocess that calls the provided sample.py script: 

python sample.py \ 

-c CFG \ 

- -m CKPT \ 

-s 1 2 H W \ 

- -t 100 \ 

- -o out_prefix 

where -s 1 2 H W represents a tensor of shape (batch size = 1 _,_ depth = 2 _, 𝐻,𝑊_ ), or in other words, a single 2D point set laid out 

Conference’17, July 2017, Washington, DC, USA 

Matthew McConnell 

on a grid. -t 100 uses 100 **reverse diffusion steps** as a qualityperformance compromise, where the original paper often used up to **1000 steps** . This run of sample.py reconstructs the model from a stored JSON config, loads checkpoint weights, and executes the denoising loop. The result is stored as a NumPy .npy file. 

The FastAPI server then loads this .npy file into memory, optionally down sampling it to **exactly** N points. As the author’s current repository only allows a specific number of points, I have elected to apply a uniform random sub-selection to select a subset of the generated pattern, to ensure that a sample is provided regardless of it’s validity with the model. However, this random sub-sampling often completely degenerates the underlying statistics or properties of the learned samplers, and should ideally never be used for high-quality renders. As such, the "valid" sample counts for the learned model are **64, 256, 576** , but the user technically has the options of: **8, 16, 32, 64, 256, 576** . This choice was intentional, as it’s interesting to see the degredation of DoF quality at lower samples, and the analytical sampling patterns (White Noise and Hammersley) can generate correctly at these reduced counts. 

To provide comparables to "more traditional" samplers (such as the ones seen in class), the Python back-end further provides two analytical samplers which have been implemented directly in Python: 

- WhiteNoise: Uniform random samples in [0 _,_ 1]<sup>2</sup> 

- Hammersley: A low-discrepancy sequence using the standard construction and a base-2 van der Corput radical inverse for the second coordinate. 

The above samplers are handled independently of the learned, diffusion-based samplers, and are presented as useful comparisons. 

In terms of Unity-FastAPI connection, I treat this as a bokeh kernel "black box". A separate FastAPI "client" script (in Unity) sends a sample request via UnityWebRequests, which requests a sampler name, and _𝑁_ points. If the request is successful, the server responds with a JSON file containing the sampler points. The returned ( _𝑥,𝑦_ ) samples in [0 _,_ 1]<sup>2</sup> are remapped to [−1 _,_ 1]<sup>2</sup> and packed into a list which is then stored in the Unity representation of the kernel. Given a sample set _𝑆_ , with length _𝑁_ , the kernel is created as: 

### _𝐾_ [ _𝑖_ ] = [( _𝑆_ [ _𝑖_ ] _𝑥_ − 0 _._ 5) · 2 _,_ ( _𝑆_ [ _𝑖_ ] _𝑦_ − 0 _._ 5) · 2 _,_ 0 _,_ 0] 

where _𝑆_ [ _𝑖_ ] _𝑥_ and _𝑆_ [ _𝑖_ ] _𝑦_ represent the ( _𝑥,𝑦_ ) sample location for a given index in the parsed JSON list. The result is then stored in a custom Kernel container, where the Kernel Count and _Kernel variables are correspondingly set in the shader. _𝐾_ [ _𝑖_ ] then represents a 2D offset direction for gathering neighboring pixels in the DoF shader (see above for details). In other words, the diffusion model’s output (as supplied by Python) becomes the **aperture sampling pattern** of the thin-lens approximation. 

Notably, this update happens whenever the user changes the active sampler, requests a different number of points, or at startup - providing a fresh sampler set from the learned model at **each update** . Such an architectural setup attempts to decouple the learned model and rendering processes - where the heavy diffusion model runs in an out-of-process Python service, and Unity only sees a lightweight array of 2D directions. 



**Figure 5: Reference Scene Image (DoF Disabled)** 



**Figure 6: White noise (left) vs. Poisson (right) - 64 samples** 

## **5 EVALUATION & RESULTS** 

## **5.1 Qualitative Description of Bokeh Structure** 

To assess the visual or perceptual behaviour of the varying sampling patterns, I compare the resulting DoF blur produced by each sampler across multiple kernel sizes. All discussions revolve on various screenshots with relation to a reference image of a coffee shop, as seen in Figure 5. 

White-noise sampling produces irregular blur patches with a noticeable grainy appearance, particularly in out of focus regions such as the lights. The underlying randomized sampling pattern is much more visible when compared to that of the blue-noise sampling (Figure 6). Additionally, the white-noise sampler introduces high-variance pixel to pixel fluctuations that the post-filter pass cannot fully smooth. 

Conversely, low-discrepancy sequences such as Hammersley exhibit smoother blur - but introduce subtle streaking or anisotropic patterns due to the structure nature of their stratification. Figure 7 showcases this well. In particular, a clear "directional bias" can be noticed in the blur, where the Hammersley sampler produces more of a stretched diamond shape compared to the less structured, but equally low-discrepancy nature of Owen. Blue-noise based samplers (such as LDBN and GBN) generate the cleanest and most "visually appealing" bokeh. At higher sampling counts, Owen and Poisson produce low discrepancy patterns, with Poisson producing a very close "blue-noise esque" power spectrum. Out of focus highlights are more uniform, low-frequency noise is strongly suppressed, and the blur exhibits stable and visually cohesive textures. This aligns well with the expected characteristics of blue-noise, 

Conference’17, July 2017, Washington, DC, USA 

Example-Based Sampling for Depth of Field in Unity 



<!-- Start of picture text -->
7<br>Oy ® :<br><!-- End of picture text -->

**Figure 7: Hammersley (left) vs. Owens (right) - 64 samples** 



<!-- Start of picture text -->
ae s.<br>™<br>i<br>Bes,<br>2“ ‘ee<br><!-- End of picture text -->

**Figure 8: LDBN (top left), GBN (top right), Poisson (bottom left), Owen (bottom right) - 576 samples** 



**Figure 9: LDBN at 64, 256 and 576 samples (from left to right)** 

yielding visually pleasing, smooth, and grain-free blur results. All four diffusion-based samplers closely match these characteristics, with the resultant bokeh closely resembling what might be seen in analytical blue-noise distributions. 

Finally, to motivate a discussion surrounding varying sample counts, Figure 9 showcases LDBN at several kernel sizes. When using a high-quality blue-noise sampler, as little as 64 samples is sufficient to produce visually appealing blur. Notably, as the samples increase from 64 to 256, there is a clear improvement, where grain is reduced and out-of-focus highlights are stabilized. As this is increased to 576 the blur becomes slightly smoother, but 



<!-- Start of picture text -->
DoF Semper Convergence (RMSE vs sample count)<br>~ =<br>Sax = Hanmersey<br>. = oven<br>Ss \ S= rosson<br>Bo$10g \\SS.<br>~<br>a<br>we we<br><!-- End of picture text -->

**Figure 10: RMSE Convergence of all samplers across all sample counts. Shown on a logarithmic axis.** 



<!-- Start of picture text -->
DoF Sampler Convergence (RMSE vs sample count)<br>1<br>2<br>3: Sane=e<br>3 ~~<br>j~*<br>2| cn mks<br>S ammestey<br>eae SOT<br>= oven<br>-e Whitenoise “<br>Pa<br><!-- End of picture text -->

**Figure 11: RMSE Convergence of all samplers across valid learned sample counts. Shown on a logarithmic axis.** 

the improvement is far less pronounced. This aligns well with bluenoise characteristics, whose low-frequency suppression already provides strong performance at relatively small samples. 

## **5.2 Quantitative RMSE Convergence** 

To supplement the qualitative evaluation, I further evaluate each sampler quantitatively by calculating the Root Mean Square Error (RMSE) of each sampler against a reference "ground-truth" image. This ground-truth image was rendered with the LDBN (blue-noise) sampler at 576 samples. For each sampler and kernel size _𝑁_ , I computed the RMSE between the rendered image and the ground truth. The resulting convergence curves (shown in Figures 10 and 11) were plotted on a logarithmic scale. Two graphs are shown here. First, Figure 10 which showcases **all** sample counts, whether they could succesfully be created by the learned model or not. This clearly introduces noise between the sampling counts. Although this clearly shows the trend, a better representation can be seen in Figure 11, in which only the valid sampler counts (64 _,_ 256 _,_ 576) are shown. Finally, both graphs see the LDBN sampler reach the ground truth value abruptly. This is expected, as the ground truth **is** the LDBN sampler at _𝑛_ = 576. 

Conference’17, July 2017, Washington, DC, USA 

Matthew McConnell 

White-noise performs significantly worse than all structured samplers across the entire sample range. Its RMSE curve decreases only gradually, reflecting the expected _𝑂_ ( _𝑁_<sup>−1/2</sup> ) convergence of random samplers [3]. Additionally, the magnitude of the RMSE is much higher than the comparables. Even at the highest sample count of 576, white noise remains almost an order of magnitude higher in error compared to the low-discrepancy methods. 

Conversely, all structured samplers (LDBN, GBN, Hammersley, Owen, and Poisson) exhibit substantially faster convergence. Notably, LDBN and GBN consistently achieve the lowest error, particularly in the 64 - 256 sample space where blue-noise distributions provide a strong advantage by suppressing low-frequency error [10]. As the convergence nears 576 samples, all of the low-discrepancy samplers perform virtually the same, indicating that these samplers are essentially indistinguishable (in terms of RMSE) at extremely high sample counts. This is visually verified in Figure 8. 

Interestingly, Poisson produces moderate error and slower convergence compared to the other low-discrepancy samplers. This is consistent with the underlying behaviour of Poisson’s "blue-noise esque" spectral properties, where the spectra resembles blue-noise distributions, except for the fact that they do not decrease towards zero as the frequency decreases [5]. 

Hammersley and Owen sequences perform relatively competitively, with curves falling between the blue-noise samplers and Poisson. Notably, Owen performs better at the 64 to 256 sample range, but post 256, they are relatively identical. Their structured stratification allows them to converge smoothly and predictably, although as seen in the qualitative section, there can be noticeable visual "bias", particularly in the shape of the bokeh. 

## **5.3 Discussion** 

Both the qualitative and quantitative evaluations highlight the importance of well-chosen sample distributions in DoF rendering. Sampling patterns that suppress low-frequency noise, such as blue-noise and other low-discrepancy sequences, produce visibly smoother bokeh and lower RMSE compared to unstructured methods such as white noise. Across all samplers, the RMSE curves (Figures 10, 11) demonstrate clear diminishing returns beyond 256 samples - especially for blue-noise distributions whose variance is already low at tolerable kernel sizes. 

Among all tested samplers, the blue-noise based samplers (LDBN and GBN) produced the most visually pleasing DoF effects. Both samplers are high quality, isotropic point sets that produce clean bokeh even at modest sample counts. However, as the samples increase, the difference between samplers is nearly indistinguishable - particularly for samplers provided by the learned model. 

## **6 CONCLUSION** 

In this work, I utilized Doignies et al’s. [5] example-based diffusion model to generate sampling kernels for real-time depth of field in the Unity game engine. A custom four pass depth of field implementation, powered by Unity’s Universal Render Pipeline provided an excellent testbed for such a comparison. Both qualitative and quantitative comparisons have been included, in an attempt to describe and measure the various properties, differences, strengths, 

and overall characteristics of multiple samplers at varying sample counts. 

## **6.1 Limitations** 

Although the Python to Unity connection is fast enough for this prototype, it’s not inherently "real-time". Users must press a button to obtain a new sample set, and it takes a few seconds to load. To avoid this, kernels could be pre-computed before hand and read into memory when needed. However, I wanted the functionality of generating a fresh point set on each request, requiring a call to the learned model. A trivial extension would be to run Unity and the sampler on the same system, such that all of the networking can be entirely avoided. Additionally, analysis on variance / underlying measured properties of the point-sets would help motivate the discussion between differences in samplers. Doignies et al. [5] provide such a comparison, as such it is left as future reading. 

## **6.2 Future Work** 

There are a few avenues for future work. First, the depth of field effect is relatively trivial - running in four passes and on one shader. There are many such extensions to this effect, such as modifying the aperture shape, adding additional filtering passes or, applying a weighted average to tone down the bokeh / make it a bit more controllable. Additionally, towards the end of this project I began investigating "Temporal Accumulation" which essentially achieves a depth of field effect by accumulating multiple exposures or frames over time. This provides a unique extension, where a different sampler could be used between frames, possibly producing interesting results. 

## **REFERENCES** 

- [1] Abdalla G. M. Ahmed, Hélène Perrier, David Coeurjolly, Victor Ostromoukhov, Jianwei Guo, Dong-Ming Yan, Hui Huang, and Oliver Deussen. 2016. Lowdiscrepancy blue noise sampling. _ACM Trans. Graph._ 35, 6, Article 247 (Dec. 2016), 13 pages. https://doi.org/10.1145/2980179.2980218 

- [2] Abdalla G. M. Ahmed, Jing Ren, and Peter Wonka. 2022. Gaussian Blue Noise. _ACM Trans. Graph._ 41, 6, Article 260 (Nov. 2022), 15 pages. https://doi.org/10. 1145/3550454.3555519 

- [3] Robert L Cook. 1986. Stochastic sampling in computer graphics. _ACM Transactions on Graphics (TOG)_ 5, 1 (1986), 51–72. 

- [4] Joe Demers. 2007. Chapter 23. Depth of Field: A Survey of Techniques — developer.nvidia.com. https://developer.nvidia.com/gpugems/gpugems/part-iv-imageprocessing/chapter-23-depth-field-survey-techniques. [Accessed 11-12-2025]. 

- [5] Bastien Doignies, Nicolas Bonneel, David Coeurjolly, Julie Digne, Loïs Paulin, Jean-Claude Iehl, and Victor Ostromoukhov. 2023. Example-based sampling with diffusion models. In _SIGGRAPH Asia 2023 Conference Papers_ . 1–11. 

- [6] Bastien Doignies, Nicolas Bonneel, David Coeurjolly, Julie Digne, Loïs Paulin, Jean-Claude Iehl, and Victor Ostromoukhov. 2023. GitHub - BDoignies/ExampleBasedSamplingWithDiffusion: Code for the paper "Example-Based Sampling with Diffusion Models" — github.com. https://github.com/BDoignies/ ExampleBasedSamplingWithDiffusion. [Accessed 11-12-2025]. 

- [7] Jasper Flick. 2018. Depth of Field — catlikecoding.com. https://catlikecoding. com/unity/tutorials/advanced-rendering/depth-of-field/. [Accessed 11-12-2025]. 

- [8] Dennis Gustafsson. 2018. The importance of good noise. https://blog.voxagon. se/2018/12/07/the-importance-of-good-noise.html 

- [9] Daniel Heck, Thomas Schlömer, and Oliver Deussen. 2013. Blue noise sampling with controlled aliasing. _ACM Transactions on Graphics (TOG)_ 32, 3 (2013), 1–12. 

- [10] Hongwei Li, Li-Yi Wei, Pedro V Sander, and Chi-Wing Fu. 2010. Anisotropic blue noise sampling. In _ACM SIGGRAPH Asia 2010 papers_ . 1–12. 

- [11] SA Lloyd. 1983. An optimization approach to relaxation labelling algorithms. _Image and Vision Computing_ 1, 2 (1983), 85–91. 

- [12] Matt Pharr, Wenzel Jakob, and Greg Humphreys. 2023. _Physically Based Rendering: From Theory to Implementation_ . https://www.pbr-book.org/ 

- [13] Xin Sun, Kun Zhou, Jie Guo, Guofu Xie, Jingui Pan, Wencheng Wang, and Baining Guo. 2013. Line segment sampling with blue-noise properties. _ACM Trans. Graph._ 32, 4 (2013), 127–1. 

