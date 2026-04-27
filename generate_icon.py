from PIL import Image, ImageDraw

def create_security_icon():
    # Tamanho do ícone (padrão para alta resolução)
    size = (256, 256)
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # Cores
    dark_blue = (15, 30, 60, 255)
    light_blue = (30, 80, 150, 255)
    white = (240, 240, 240, 255)
    silver = (192, 192, 192, 255)

    # Desenhar o Escudo
    shield_points = [
        (128, 20),   # Topo Centro
        (220, 50),   # Topo Direita
        (200, 180),  # Lado Direita
        (128, 240),  # Ponta Baixo
        (56, 180),   # Lado Esquerda
        (36, 50),    # Topo Esquerda
    ]
    draw.polygon(shield_points, fill=dark_blue, outline=silver, width=5)

    # Desenhar Detalhe Interno do Escudo
    inner_shield = [
        (128, 40), (200, 65), (185, 170), (128, 220), (71, 170), (56, 65)
    ]
    draw.polygon(inner_shield, fill=light_blue)

    # Desenhar o Cadeado
    # Base do cadeado
    draw.rounded_rectangle([90, 110, 166, 160], radius=5, fill=white)
    # Alça do cadeado
    draw.arc([100, 80, 156, 130], start=180, end=0, fill=white, width=8)

    # Buraco da fechadura
    draw.ellipse([120, 125, 136, 141], fill=dark_blue)
    draw.rectangle([125, 135, 131, 150], fill=dark_blue)

    # Salvar em múltiplos tamanhos para formato .ico
    icon_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    image.save("Resources/app_icon.ico", format="ICO", sizes=icon_sizes)

if __name__ == "__main__":
    create_security_icon()
    print("Ícone gerado com sucesso em Resources/app_icon.ico")
